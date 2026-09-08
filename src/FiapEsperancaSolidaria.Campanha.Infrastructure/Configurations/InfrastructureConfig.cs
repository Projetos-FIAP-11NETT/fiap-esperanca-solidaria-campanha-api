using Amazon.S3;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Storage;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Cache;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Configurations;

public static class InfrastructureConfig
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IDonationRepository, DonationRepository>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddHangfire(config => config
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));
        services.AddHangfireServer();

        services.Configure<S3Settings>(configuration.GetSection("S3Settings"));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<S3Settings>>().Value;

            if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
            {
                return new AmazonS3Client(settings.AccessKey, settings.SecretKey, new AmazonS3Config
                {
                    ServiceURL = settings.ServiceUrl,
                    ForcePathStyle = true,
                    AuthenticationRegion = settings.Region,
                });
            }

            // AWS real: sem credenciais explícitas, usa a IAM role do node via IMDS.
            return new AmazonS3Client(new AmazonS3Config { AuthenticationRegion = settings.Region });
        });
        services.AddSingleton<IImageStorageService, S3ImageStorageService>();

        return services;
    }
}
