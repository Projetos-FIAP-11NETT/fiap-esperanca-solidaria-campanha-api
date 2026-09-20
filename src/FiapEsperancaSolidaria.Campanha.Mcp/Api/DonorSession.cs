using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol;

namespace FiapEsperancaSolidaria.Campanha.Mcp.Api;

// Faz login do doador na usuario-api e mantém o token em memória até perto de expirar.
// As credenciais vêm de variáveis de ambiente do processo do servidor MCP, nunca do chat:
// senha passando pelo modelo ficaria no histórico da conversa.
public sealed class DonorSession(HttpClient usuarioApi, McpSettings settings)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasValidToken())
            return _token!;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (HasValidToken())
                return _token!;

            if (string.IsNullOrWhiteSpace(settings.DonorEmail) || string.IsNullOrWhiteSpace(settings.DonorPassword))
                throw new McpException("Conta do doador não configurada: defina DONOR_EMAIL e DONOR_PASSWORD nas variáveis de ambiente do servidor MCP.");

            HttpResponseMessage response;
            try
            {
                response = await usuarioApi.PostAsJsonAsync(
                    "api/v1/User/Login",
                    new { email = settings.DonorEmail, password = settings.DonorPassword },
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new McpException($"Não foi possível acessar a usuario-api em {usuarioApi.BaseAddress}: {ex.Message}", ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                    throw new McpException($"Login na usuario-api falhou (HTTP {(int)response.StatusCode}). Confira DONOR_EMAIL e DONOR_PASSWORD.");

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
