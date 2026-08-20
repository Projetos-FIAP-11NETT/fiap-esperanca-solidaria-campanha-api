using Microsoft.OpenApi;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations.OpenApi;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddOpenApiConfiguration(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Conexão Solidária - Campanha API",
                    Version = "v1",
                    Description = "Gestão de campanhas, painel de transparência e recebimento de intenções de doação.",
                    Contact = new OpenApiContact
                    {
                        Name = "FIAP Conexão Solidária Team",
                        Email = "contato@fiapconexaosolidaria.com"
                    }
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }
}