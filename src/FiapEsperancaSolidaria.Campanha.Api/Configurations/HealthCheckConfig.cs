using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations;

public static class HealthCheckConfig
{
    private static readonly string[] tags = ["db", "postgres", "ready"];

    public static IServiceCollection AddHealthCheckConfiguration(this IServiceCollection services,IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString!,
                name: "campaign-db",
                failureStatus: HealthStatus.Unhealthy,
                tags: tags);

        return services;
    }

    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
        endpoints.MapHealthChecks("/health/ready");
        endpoints.MapHealthChecks("/health/live");

        return endpoints;
    }
}