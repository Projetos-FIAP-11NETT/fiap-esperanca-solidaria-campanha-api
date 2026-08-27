using System.Reflection;
using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FiapEsperancaSolidaria.Campanha.Application.Configurations;

public static class ApplicationConfig
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        return services;
    }
}
