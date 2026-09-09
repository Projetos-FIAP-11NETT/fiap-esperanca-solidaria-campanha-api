namespace FiapEsperancaSolidaria.Campanha.Application.DTOs;

public record PublicCampaignResponse(
    Guid Id,
    string Title,
    string Description,
    string? Image,
    decimal FinancialGoal,
    decimal TotalRaised);
