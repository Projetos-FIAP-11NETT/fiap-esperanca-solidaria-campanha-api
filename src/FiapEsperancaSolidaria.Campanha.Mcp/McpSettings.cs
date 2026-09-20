using Microsoft.Extensions.Configuration;

namespace FiapEsperancaSolidaria.Campanha.Mcp;

public sealed class McpSettings
{
    public string CampanhaApiUrl { get; init; } = "http://localhost:5054";
    public string UsuarioApiUrl { get; init; } = "http://localhost:5043";
    public string? DonorEmail { get; init; }
    public string? DonorPassword { get; init; }

    public static McpSettings From(IConfiguration configuration) => new()
    {
        CampanhaApiUrl = configuration["CAMPANHA_API_URL"] ?? "http://localhost:5054",
        UsuarioApiUrl = configuration["USUARIO_API_URL"] ?? "http://localhost:5043",
        DonorEmail = configuration["DONOR_EMAIL"],
        DonorPassword = configuration["DONOR_PASSWORD"]
    };

    public static Uri AsBaseAddress(string url) => new(url.TrimEnd('/') + "/");
}
