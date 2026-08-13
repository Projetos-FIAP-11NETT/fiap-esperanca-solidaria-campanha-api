using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Configurations;

public static class InfrastructureConfig
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICampanhaRepository, CampanhaRepository>();

        return services;
    }
}
