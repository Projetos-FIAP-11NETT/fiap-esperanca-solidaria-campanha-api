using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol;

namespace FiapEsperancaSolidaria.Campanha.Mcp.Api;

public sealed class CampanhaApiClient(HttpClient http, DonorSession session)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<List<PublicCampaign>> SearchPublicCampaignsAsync(string? title, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(title)
            ? "api/v1/Campaign/public"
            : $"api/v1/Campaign/public?title={Uri.EscapeDataString(title.Trim())}";

        return SendAsync<List<PublicCampaign>>(HttpMethod.Get, path, body: null, authenticated: false, cancellationToken);
    }

    public Task<Campaign> GetCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default) =>
        SendAsync<Campaign>(HttpMethod.Get, $"api/v1/Campaign/{campaignId}", body: null, authenticated: false, cancellationToken);

    public Task<List<DonationReceipt>> ListMyDonationsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<List<DonationReceipt>>(HttpMethod.Get, "api/v1/Donation/me", body: null, authenticated: true, cancellationToken);

    public Task<Donation> GetDonationAsync(Guid donationId, CancellationToken cancellationToken = default) =>
        SendAsync<Donation>(HttpMethod.Get, $"api/v1/Donation/{donationId}", body: null, authenticated: true, cancellationToken);

    // O DonorId não é enviado: a campanha-api pega do token do doador logado.
    public Task<Donation> CreateDonationAsync(Guid campaignId, decimal amount, string paymentMethod, CancellationToken cancellationToken = default) =>
        SendAsync<Donation>(
            HttpMethod.Post,
            "api/v1/Donation",
            new { campaignId, amount, paymentMethod },
            authenticated: true,
            cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, bool authenticated, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);

            if (body is not null)
                request.Content = JsonContent.Create(body, options: Json);

            if (authenticated)
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
                if (response.StatusCode == HttpStatusCode.Unauthorized && authenticated && attempt == 0)
                {
                    session.Invalidate();
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    throw new McpException(await DescribeErrorAsync(response, cancellationToken));

                return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
                    ?? throw new McpException("A campanha-api devolveu uma resposta vazia.");
            }
        }
    }

    private static async Task<string> DescribeErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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
            HttpStatusCode.Unauthorized => apiMessage ?? "Não autenticado: confira DONOR_EMAIL e DONOR_PASSWORD, ou se esta doação pertence à conta configurada.",
            HttpStatusCode.Forbidden => "Sem permissão: a conta configurada precisa ter o perfil Doador.",
            HttpStatusCode.NotFound => apiMessage ?? "Recurso não encontrado.",
            _ => apiMessage ?? $"A campanha-api respondeu HTTP {(int)response.StatusCode}."
        };
    }
}
