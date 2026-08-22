namespace FiapEsperancaSolidaria.Campanha.Application.Behaviors;

public static class CacheKeys
{
    public static string CampanhasPublicas(string? titulo = null) =>
        string.IsNullOrWhiteSpace(titulo) ? "campanhas:publicas" : $"campanhas:publicas:titulo={titulo.Trim().ToLowerInvariant()}";

    public static string Campanha(Guid id) => $"campanha:{id}";
}
