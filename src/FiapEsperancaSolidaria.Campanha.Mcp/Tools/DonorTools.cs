using System.ComponentModel;
using System.Text.Json;
using FiapEsperancaSolidaria.Campanha.Mcp.Api;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FiapEsperancaSolidaria.Campanha.Mcp.Tools;

// Todas as tools daqui agem como o doador configurado em DONOR_EMAIL/DONOR_PASSWORD.
[McpServerToolType]
public sealed class DonorTools(CampanhaApiClient api)
{
    private static readonly string[] PaymentMethods = ["CreditCard", "DebitCard", "Pix", "Boleto"];
    private static readonly string[] InProgressStatuses = ["Pending", "PaymentProcessing"];

    [McpServerTool(Name = "my_donations", Title = "Meu recibo de doações", ReadOnly = true, OpenWorld = false)]
    [Description("Recibo do doador logado: todas as doações que ele já fez, da mais recente para a mais antiga, com título e status da campanha — inclusive campanhas que já foram concluídas ou canceladas. Traz também os totais (aprovado, em andamento, recusado).")]
    public async Task<string> MyDonations(CancellationToken cancellationToken = default)
    {
        var receipts = await api.ListMyDonationsAsync(cancellationToken);

        return JsonSerializer.Serialize(new
        {
            summary = new
            {
                donations = receipts.Count,
                campaignsSupported = receipts.Select(r => r.CampaignId).Distinct().Count(),
                campaignsNoLongerActive = receipts.Where(r => r.CampaignStatus != "Active").Select(r => r.CampaignId).Distinct().Count(),
                totalApproved = receipts.Where(r => r.Status == "Approved").Sum(r => r.Amount),
                totalInProgress = receipts.Where(r => InProgressStatuses.Contains(r.Status)).Sum(r => r.Amount),
                totalRejected = receipts.Where(r => r.Status == "Rejected").Sum(r => r.Amount)
            },
            donations = receipts
        }, CampaignTools.JsonOptions);
    }

    [McpServerTool(Name = "get_donation", Title = "Ver doação", ReadOnly = true, OpenWorld = false)]
    [Description("Detalhes de uma doação pelo id. Só funciona para doações do próprio doador logado.")]
    public async Task<string> GetDonation(
        [Description("Id (GUID) da doação.")] Guid donationId,
        CancellationToken cancellationToken = default)
    {
        var donation = await api.GetDonationAsync(donationId, cancellationToken);
        return JsonSerializer.Serialize(donation, CampaignTools.JsonOptions);
    }

    [McpServerTool(Name = "donate", Title = "Fazer uma doação", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Registra uma doação do doador logado para uma campanha ativa. É uma ação com efeito real: SEMPRE chame primeiro com confirm=false para obter a prévia, mostre ao usuário (campanha, valor, forma de pagamento) e só chame com confirm=true depois de ele confirmar explicitamente. A doação nasce com status Pending; a aprovação do pagamento acontece depois, em outro serviço.")]
    public async Task<string> Donate(
        [Description("Id (GUID) da campanha que vai receber a doação.")] Guid campaignId,
        [Description("Valor em reais (BRL), maior que zero.")] decimal amount,
        [Description("Forma de pagamento: CreditCard, DebitCard, Pix ou Boleto.")] string paymentMethod,
        [Description("false = só mostra a prévia, nada é doado. true = efetiva a doação (somente após confirmação explícita do usuário).")] bool confirm = false,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new McpException("O valor da doação deve ser maior que zero.");

        var method = PaymentMethods.FirstOrDefault(m => string.Equals(m, paymentMethod?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new McpException($"Forma de pagamento inválida: '{paymentMethod}'. Use uma destas: {string.Join(", ", PaymentMethods)}.");

        var campaign = await api.GetCampaignAsync(campaignId, cancellationToken);
        if (campaign.Status != "Active")
            throw new McpException($"A campanha \"{campaign.Title}\" não está ativa (status: {campaign.Status}) e não recebe doações.");

        if (!confirm)
        {
            return JsonSerializer.Serialize(new
            {
                donated = false,
                needsConfirmation = true,
                message = "Nenhuma doação foi feita. Mostre a prévia ao usuário e, só se ele confirmar explicitamente, chame donate de novo com confirm=true.",
                preview = new
                {
                    campaignId,
                    campaignTitle = campaign.Title,
                    amount,
                    paymentMethod = method
                }
            }, CampaignTools.JsonOptions);
        }

        var donation = await api.CreateDonationAsync(campaignId, amount, method, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            donated = true,
            campaignTitle = campaign.Title,
            donation
        }, CampaignTools.JsonOptions);
    }
}
