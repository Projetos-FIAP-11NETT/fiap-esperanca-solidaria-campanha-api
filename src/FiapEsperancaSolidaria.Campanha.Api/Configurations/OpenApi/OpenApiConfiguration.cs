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

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Informe o token JWT do Firebase no formato: Bearer {token}"
                };

                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
