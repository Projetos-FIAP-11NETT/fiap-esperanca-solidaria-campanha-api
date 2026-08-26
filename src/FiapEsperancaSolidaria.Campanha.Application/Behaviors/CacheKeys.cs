namespace FiapEsperancaSolidaria.Campanha.Application.Behaviors;

public static class CacheKeys
{
    public static string PublicCampaigns(string? title = null) =>
        string.IsNullOrWhiteSpace(title) ? "campaigns:public" : $"campaigns:public:title={title.Trim().ToLowerInvariant()}";

    public static string Campaign(Guid id) => $"campaign:{id}";
}
