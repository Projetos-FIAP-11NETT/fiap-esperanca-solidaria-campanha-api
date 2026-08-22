using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Configurations;

public static class InfrastructureConfig
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICampanhaRepository, CampanhaRepository>();
        services.AddScoped<IDonationRepository, DonationRepository>();

        return services;
    }
}
