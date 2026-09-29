using Scalar.AspNetCore;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations.OpenApi;

public static class OpenApiPipeline
{
    public static IEndpointRouteBuilder MapOpenApiConfiguration(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApi();
        endpoints.MapScalarApiReference("/docs");
        return endpoints;
    }
}
