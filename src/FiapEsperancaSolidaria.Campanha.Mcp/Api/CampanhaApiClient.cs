using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol;

namespace FiapEsperancaSolidaria.Campanha.Mcp.Api;

public sealed class CampanhaApiClient(HttpClient http, AccountSession donor, AccountSession manager)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // --- público (sem login) ---

    public Task<List<PublicCampaign>> SearchPublicCampaignsAsync(string? title, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(title)
            ? "api/v1/Campaign/public"
            : $"api/v1/Campaign/public?title={Uri.EscapeDataString(title.Trim())}";

        return SendAsync<List<PublicCampaign>>(HttpMethod.Get, path, body: null, session: null, cancellationToken);
    }

    public Task<Campaign> GetCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default) =>
        SendAsync<Campaign>(HttpMethod.Get, $"api/v1/Campaign/{campaignId}", body: null, session: null, cancellationToken);

    // --- doador ---

    public Task<List<DonationReceipt>> ListMyDonationsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<List<DonationReceipt>>(HttpMethod.Get, "api/v1/Donation/me", body: null, donor, cancellationToken);

    public Task<Donation> GetDonationAsync(Guid donationId, CancellationToken cancellationToken = default) =>
        SendAsync<Donation>(HttpMethod.Get, $"api/v1/Donation/{donationId}", body: null, donor, cancellationToken);

    // O DonorId não é enviado: a campanha-api pega do token do doador logado.
    public Task<Donation> CreateDonationAsync(Guid campaignId, decimal amount, string paymentMethod, CancellationToken cancellationToken = default) =>
        SendAsync<Donation>(
            HttpMethod.Post,
            "api/v1/Donation",
            new { campaignId, amount, paymentMethod },
            donor,
            cancellationToken);

    // --- gestor da ONG ---

    public Task<List<Campaign>> ListAllCampaignsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<List<Campaign>>(HttpMethod.Get, "api/v1/Campaign", body: null, manager, cancellationToken);

    public Task<Campaign> CreateCampaignAsync(
        string title,
        string description,
        DateOnly startDate,
        DateOnly endDate,
        decimal financialGoal,
        CancellationToken cancellationToken = default) =>
        SendAsync<Campaign>(
            HttpMethod.Post,
            "api/v1/Campaign",
            new
            {
                title,
                description,
                startDate = startDate.ToString("yyyy-MM-dd"),
                endDate = endDate.ToString("yyyy-MM-dd"),
                financialGoal,
                image = (string?)null
            },
            manager,
            cancellationToken);

    public Task<Campaign> CancelCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default) =>
        SendAsync<Campaign>(HttpMethod.Post, $"api/v1/Campaign/{campaignId}/cancel", body: null, manager, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, AccountSession? session, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);

            if (body is not null)
                request.Content = JsonContent.Create(body, options: Json);

            if (session is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await session.GetTokenAsync(cancellationToken));

            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new McpException($"Não foi possível acessar a campanha-api em {http.BaseAddress}: {ex.Message}", ex);
            }

            using (response)
            {
                // Token pode ter expirado antes do previsto: descarta e tenta mais uma vez com login novo.
                if (response.StatusCode == HttpStatusCode.Unauthorized && session is not null && attempt == 0)
                {
                    session.Invalidate();
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    throw new McpException(await DescribeErrorAsync(response, session, cancellationToken));

                return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
                    ?? throw new McpException("A campanha-api devolveu uma resposta vazia.");
            }
        }
    }

    private static async Task<string> DescribeErrorAsync(HttpResponseMessage response, AccountSession? session, CancellationToken cancellationToken)
    {
        // A campanha-api devolve os erros de negócio como { "error": "mensagem" }.
        string? apiMessage = null;
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("error", out var error))
                apiMessage = error.GetString();
        }
        catch (JsonException)
        {
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => apiMessage ?? $"Não autenticado ou sem acesso a este recurso: confira {session?.CredentialsHint ?? "as credenciais"}.",
            HttpStatusCode.Forbidden => $"Sem permissão: a conta configurada precisa ter o perfil {session?.RequiredRole ?? "adequado"}.",
            HttpStatusCode.NotFound => apiMessage ?? "Recurso não encontrado.",
            _ => apiMessage ?? $"A campanha-api respondeu HTTP {(int)response.StatusCode}."
        };
    }
}
