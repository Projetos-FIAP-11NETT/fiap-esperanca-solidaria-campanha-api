using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol;

namespace FiapEsperancaSolidaria.Campanha.Mcp.Api;

// Faz login de uma conta (doador ou gestor) na usuario-api e mantém o token em memória até
// perto de expirar. As credenciais vêm de variáveis de ambiente do processo do servidor MCP,
// nunca do chat: senha passando pelo modelo ficaria no histórico da conversa.
public sealed class AccountSession(
    HttpClient usuarioApi,
    string? email,
    string? password,
    string credentialsHint,
    string requiredRole)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    // Ex.: "DONOR_EMAIL e DONOR_PASSWORD" — usado nas mensagens de erro.
    public string CredentialsHint => credentialsHint;

    // Perfil que a conta precisa ter (Doador ou GestorONG) — usado na mensagem de 403.
    public string RequiredRole => requiredRole;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasValidToken())
            return _token!;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (HasValidToken())
                return _token!;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new McpException($"Conta ({requiredRole}) não configurada: defina {credentialsHint} nas variáveis de ambiente do servidor MCP.");

            HttpResponseMessage response;
            try
            {
                response = await usuarioApi.PostAsJsonAsync(
                    "api/v1/User/Login",
                    new { email, password },
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new McpException($"Não foi possível acessar a usuario-api em {usuarioApi.BaseAddress}: {ex.Message}", ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                    throw new McpException($"Login na usuario-api falhou (HTTP {(int)response.StatusCode}). Confira {credentialsHint}.");

                var login = await response.Content.ReadFromJsonAsync<LoginResult>(Json, cancellationToken)
                    ?? throw new McpException("Resposta de login vazia da usuario-api.");

                _token = login.IdToken;
                // Margem de 60s pra não mandar um token que expira no meio da requisição.
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(login.ExpiresIn - 60);
                return _token;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _token = null;

    private bool HasValidToken() => _token is not null && DateTimeOffset.UtcNow < _expiresAt;
}
