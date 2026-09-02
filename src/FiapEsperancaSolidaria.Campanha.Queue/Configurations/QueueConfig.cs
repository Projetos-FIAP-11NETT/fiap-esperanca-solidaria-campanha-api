using FiapEsperancaSolidaria.Campanha.Queue.Configurations.MassTransit;
using FiapEsperancaSolidaria.Campanha.Queue.Configurations.Sqs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapEsperancaSolidaria.Campanha.Queue.Configurations;

public static class QueueConfig
{
    public static IServiceCollection AddQueueConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MassTransitSettings>(configuration.GetSection(nameof(MassTransitSettings)));
        services.Configure<SqsSettings>(configuration.GetSection(nameof(SqsSettings)));

        services.RegisterSqsStartup();

        return services;
    }
}