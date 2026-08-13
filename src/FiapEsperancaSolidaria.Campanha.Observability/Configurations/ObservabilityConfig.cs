using FiapEsperancaSolidaria.Campanha.Observability.Correlation;
using Microsoft.Extensions.DependencyInjection;

namespace FiapEsperancaSolidaria.Campanha.Observability.Configurations;

public static class ObservabilityConfig
{
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        return services;
    }
}
