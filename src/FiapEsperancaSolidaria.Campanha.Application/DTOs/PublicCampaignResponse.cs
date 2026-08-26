namespace FiapEsperancaSolidaria.Campanha.Application.DTOs;

public record PublicCampaignResponse(
    Guid Id,
    string Title,
    decimal FinancialGoal,
    decimal TotalRaised);
