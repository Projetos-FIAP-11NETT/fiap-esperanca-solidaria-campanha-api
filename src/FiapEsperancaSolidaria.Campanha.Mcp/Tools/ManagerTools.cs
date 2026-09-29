using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using FiapEsperancaSolidaria.Campanha.Mcp.Api;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FiapEsperancaSolidaria.Campanha.Mcp.Tools;

// Tools de administração: agem como a conta GestorONG de MANAGER_EMAIL/MANAGER_PASSWORD.
// Só são registradas quando essa conta está configurada (ver Program.cs).
[McpServerToolType]
public sealed class ManagerTools(CampanhaApiClient api)
{
    private static readonly string[] Statuses = ["Scheduled", "Active", "Completed", "Cancelled"];

    [McpServerTool(Name = "list_all_campaigns", Title = "Listar todas as campanhas (gestor)", ReadOnly = true, OpenWorld = false)]
    [Description("Visão do gestor da ONG: lista TODAS as campanhas, em qualquer status (Scheduled, Active, Completed, Cancelled), com meta, arrecadado e progresso. Opcionalmente filtra por status.")]
    public async Task<string> ListAllCampaigns(
        [Description("Filtro opcional de status: Scheduled, Active, Completed ou Cancelled.")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        string? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            statusFilter = Statuses.FirstOrDefault(s => string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new McpException($"Status inválido: '{status}'. Use um destes: {string.Join(", ", Statuses)}.");
        }

        var campaigns = await api.ListAllCampaignsAsync(cancellationToken);
        var filtered = statusFilter is null ? campaigns : campaigns.Where(c => c.Status == statusFilter).ToList();

        return JsonSerializer.Serialize(new
        {
            total = campaigns.Count,
            byStatus = Statuses.ToDictionary(s => s, s => campaigns.Count(c => c.Status == s)),
            shown = filtered.Count,
            campaigns = filtered.Select(c => new
            {
                c.Id,
                c.Title,
                c.Status,
                c.StartDate,
                c.EndDate,
                goal = c.FinancialGoal,
                raised = c.TotalRaised,
                percentReached = CampaignView.Percent(c.TotalRaised, c.FinancialGoal)
            })
        }, CampaignTools.JsonOptions);
    }

    [McpServerTool(Name = "create_campaign", Title = "Criar campanha (gestor)", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Cria uma campanha. Ação com efeito real e pública: SEMPRE chame primeiro com confirm=false para obter a prévia, mostre ao usuário e só chame com confirm=true depois de ele confirmar explicitamente. Se a data de início for hoje ou já passou, a campanha nasce Active; se for futura, nasce Scheduled e é ativada automaticamente pelo job diário. A data de término não pode estar no passado. Datas no formato AAAA-MM-DD.")]
    public async Task<string> CreateCampaign(
        [Description("Título da campanha (deve ser único, sem diferenciar maiúsculas/minúsculas).")] string title,
        [Description("Descrição da campanha.")] string description,
        [Description("Data de início, AAAA-MM-DD.")] string startDate,
        [Description("Data de término, AAAA-MM-DD.")] string endDate,
        [Description("Meta financeira em reais (BRL), maior que zero.")] decimal financialGoal,
        [Description("false = só mostra a prévia, nada é criado. true = cria a campanha (somente após confirmação explícita do usuário).")] bool confirm = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new McpException("O título da campanha é obrigatório.");
        if (string.IsNullOrWhiteSpace(description))
            throw new McpException("A descrição da campanha é obrigatória.");
        if (financialGoal <= 0)
            throw new McpException("A meta financeira deve ser maior que zero.");

        var start = ParseDate(startDate, "startDate");
        var end = ParseDate(endDate, "endDate");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (end < start)
            throw new McpException("A data de término não pode ser anterior à de início.");
        if (end < today)
            throw new McpException($"A data de término não pode estar no passado (hoje, em UTC, é {today:yyyy-MM-dd}).");

        var expectedStatus = start <= today ? "Active" : "Scheduled";

        if (!confirm)
        {
            return JsonSerializer.Serialize(new
            {
                created = false,
                needsConfirmation = true,
                message = "Nenhuma campanha foi criada. Mostre a prévia ao usuário e, só se ele confirmar explicitamente, chame create_campaign de novo com confirm=true.",
                todayUtc = today.ToString("yyyy-MM-dd"),
                preview = new
                {
                    title = title.Trim(),
                    description = description.Trim(),
                    startDate = start.ToString("yyyy-MM-dd"),
                    endDate = end.ToString("yyyy-MM-dd"),
                    goal = financialGoal,
                    expectedStatus
                }
            }, CampaignTools.JsonOptions);
        }

        var campaign = await api.CreateCampaignAsync(title.Trim(), description.Trim(), start, end, financialGoal, cancellationToken);

        return JsonSerializer.Serialize(new { created = true, campaign }, CampaignTools.JsonOptions);
    }

    [McpServerTool(Name = "cancel_campaign", Title = "Cancelar campanha (gestor)", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Cancela uma campanha. IRREVERSÍVEL e visível ao público: SEMPRE chame primeiro com confirm=false, mostre a prévia ao usuário e só chame com confirm=true depois de ele confirmar explicitamente. Atenção à regra de negócio: se a meta financeira já foi atingida, a campanha NÃO fica Cancelled — ela é marcada como Completed (concluída com sucesso). Campanha já concluída não pode ser cancelada.")]
    public async Task<string> CancelCampaign(
        [Description("Id (GUID) da campanha.")] Guid campaignId,
        [Description("false = só mostra a prévia, nada é cancelado. true = cancela (somente após confirmação explícita do usuário).")] bool confirm = false,
        CancellationToken cancellationToken = default)
    {
        var campaign = await api.GetCampaignAsync(campaignId, cancellationToken);

        if (campaign.Status == "Completed")
            throw new McpException($"A campanha \"{campaign.Title}\" já foi concluída e não pode ser cancelada.");
        if (campaign.Status == "Cancelled")
            throw new McpException($"A campanha \"{campaign.Title}\" já está cancelada.");

        var goalReached = campaign.TotalRaised >= campaign.FinancialGoal;

        if (!confirm)
        {
            return JsonSerializer.Serialize(new
            {
                cancelled = false,
                needsConfirmation = true,
                message = "Nenhuma alteração foi feita. Mostre a prévia ao usuário e, só se ele confirmar explicitamente, chame cancel_campaign de novo com confirm=true.",
                preview = new
                {
                    campaignId,
                    title = campaign.Title,
                    currentStatus = campaign.Status,
                    goal = campaign.FinancialGoal,
                    raised = campaign.TotalRaised,
                    expectedStatusAfter = goalReached ? "Completed" : "Cancelled",
                    note = goalReached
                        ? "A meta já foi atingida: a campanha será marcada como Completed (concluída), não como Cancelled."
                        : "A meta ainda não foi atingida: a campanha será marcada como Cancelled e deixa de receber doações."
                }
            }, CampaignTools.JsonOptions);
        }

        var result = await api.CancelCampaignAsync(campaignId, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            done = true,
            campaignId,
            title = result.Title,
            status = result.Status,
            note = result.Status == "Completed"
                ? "A meta já tinha sido atingida, então a campanha foi marcada como Completed."
                : "Campanha cancelada."
        }, CampaignTools.JsonOptions);
    }

    private static DateOnly ParseDate(string value, string field)
    {
        if (DateOnly.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        throw new McpException($"Data inválida em {field}: '{value}'. Use o formato AAAA-MM-DD (ex.: 2026-10-01).");
    }
}
