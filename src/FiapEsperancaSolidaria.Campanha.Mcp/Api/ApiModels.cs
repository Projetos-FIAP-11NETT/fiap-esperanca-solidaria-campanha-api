namespace FiapEsperancaSolidaria.Campanha.Mcp.Api;

public sealed record PublicCampaign(
    Guid Id,
    string Title,
    string Description,
    string? Image,
    decimal FinancialGoal,
    decimal TotalRaised);

public sealed record Campaign(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string? Image,
    decimal FinancialGoal,
    string Status,
    decimal TotalRaised);

public sealed record Donation(
    Guid Id,
    Guid CampaignId,
    Guid DonorId,
    decimal Amount,
    string PaymentMethod,
    string Status,
    DateTime CreatedAt);

public sealed record DonationReceipt(
    Guid DonationId,
    Guid CampaignId,
    string CampaignTitle,
    string CampaignStatus,
    decimal Amount,
    string PaymentMethod,
    string Status,
    DateTime CreatedAt);

public sealed record LoginResult(string IdToken, int ExpiresIn);
