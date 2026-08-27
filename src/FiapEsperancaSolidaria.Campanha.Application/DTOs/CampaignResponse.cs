namespace FiapEsperancaSolidaria.Campanha.Application.DTOs;

public record CampaignResponse(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string? Image,
    decimal FinancialGoal,
    string Status,
    decimal TotalRaised);
