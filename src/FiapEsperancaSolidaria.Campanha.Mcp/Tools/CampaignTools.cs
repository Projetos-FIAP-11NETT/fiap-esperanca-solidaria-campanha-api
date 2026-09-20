using System.ComponentModel;
using System.Text.Json;
using FiapEsperancaSolidaria.Campanha.Mcp.Api;
using ModelContextProtocol.Server;

namespace FiapEsperancaSolidaria.Campanha.Mcp.Tools;

[McpServerToolType]
public sealed class CampaignTools(CampanhaApiClient api)
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "search_campaigns", Title = "Buscar campanhas", ReadOnly = true, OpenWorld = false)]
    [Description("Lista as campanhas ativas do painel de transparência (público, não precisa de login). Se informar um trecho do título, filtra por ele. Valores em reais (BRL).")]
    public async Task<string> SearchCampaigns(
        [Description("Trecho do título da campanha (opcional). Ex.: \"tetraplégicos\".")] string? title = null,
        CancellationToken cancellationToken = default)
    {
        var campaigns = await api.SearchPublicCampaignsAsync(title, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            count = campaigns.Count,
            campaigns = campaigns.Select(CampaignView.From)
        }, JsonOptions);
    }

    [McpServerTool(Name = "get_campaign", Title = "Ver campanha", ReadOnly = true, OpenWorld = false)]
    [Description("Detalhes de uma campanha pelo id (público): datas, meta, quanto já foi arrecadado e status (Active, Scheduled, Completed ou Cancelled). Funciona também para campanhas já encerradas.")]
    public async Task<string> GetCampaign(
        [Description("Id (GUID) da campanha.")] Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var campaign = await api.GetCampaignAsync(campaignId, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            campaign.Id,
            campaign.Title,
            campaign.Description,
            campaign.StartDate,
            campaign.EndDate,
            campaign.Status,
            goal = campaign.FinancialGoal,
            raised = campaign.TotalRaised,
            percentReached = CampaignView.Percent(campaign.TotalRaised, campaign.FinancialGoal),
            remaining = CampaignView.Remaining(campaign.TotalRaised, campaign.FinancialGoal)
        }, JsonOptions);
    }

    [McpServerTool(Name = "transparency_summary", Title = "Resumo de transparência", ReadOnly = true, OpenWorld = false)]
    [Description("Resumo geral das campanhas ativas: quantas são, meta total, total arrecadado, percentual atingido e as 3 mais perto de bater a meta. Bom para responder \"como estamos?\".")]
    public async Task<string> TransparencySummary(CancellationToken cancellationToken = default)
    {
        var campaigns = await api.SearchPublicCampaignsAsync(title: null, cancellationToken);

        var totalGoal = campaigns.Sum(c => c.FinancialGoal);
        var totalRaised = campaigns.Sum(c => c.TotalRaised);

        return JsonSerializer.Serialize(new
        {
            activeCampaigns = campaigns.Count,
            totalGoal,
            totalRaised,
            percentReached = CampaignView.Percent(totalRaised, totalGoal),
            closestToGoal = campaigns
                .Where(c => c.TotalRaised < c.FinancialGoal)
                .OrderByDescending(c => CampaignView.Percent(c.TotalRaised, c.FinancialGoal))
                .Take(3)
                .Select(CampaignView.From)
        }, JsonOptions);
    }
}

internal static class CampaignView
{
    public static object From(PublicCampaign campaign) => new
    {
        campaign.Id,
        campaign.Title,
        campaign.Description,
        goal = campaign.FinancialGoal,
        raised = campaign.TotalRaised,
        percentReached = Percent(campaign.TotalRaised, campaign.FinancialGoal),
        remaining = Remaining(campaign.TotalRaised, campaign.FinancialGoal)
    };

    public static decimal Percent(decimal raised, decimal goal) =>
        goal <= 0 ? 0 : Math.Round(raised / goal * 100, 1);

    public static decimal Remaining(decimal raised, decimal goal) =>
        Math.Max(goal - raised, 0);
}
