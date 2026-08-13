using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations;

public static class AuthConfig
{
    public static IServiceCollection AddAuthConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var firebaseProjectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Configuração 'Firebase:ProjectId' não encontrada.");

        var authority = $"https://securetoken.google.com/{firebaseProjectId}";

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,

                    ValidateAudience = true,
                    ValidAudience = firebaseProjectId,

                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // O Firebase envia a role customizada dentro do claim "role".
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization();

        return services;
    }
}
