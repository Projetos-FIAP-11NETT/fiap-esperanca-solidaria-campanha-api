using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations;

public static class AuthConfig
{
    public static IServiceCollection AddAuthConfig(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var firebaseProjectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Configuração 'Firebase:ProjectId' não encontrada.");

        var authority = $"https://securetoken.google.com/{firebaseProjectId}";

        // A usuarios-api (emissora dos tokens Firebase) ainda não existe. Enquanto isso, em
        // Development e com Auth:DevBypassEnabled=true, aceita também um header X-Dev-Role
        // no lugar de um JWT real — nunca disponível fora de Development.
        var devBypassEnabled = environment.IsDevelopment()
            && configuration.GetValue<bool>("Auth:DevBypassEnabled");

        const string smartScheme = "Smart";
        var defaultScheme = devBypassEnabled ? smartScheme : JwtBearerDefaults.AuthenticationScheme;

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = defaultScheme;
            options.DefaultChallengeScheme = defaultScheme;
        });

        authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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

        if (devBypassEnabled)
        {
            authBuilder
                .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.SchemeName, _ => { })
                .AddPolicyScheme(smartScheme, smartScheme, options =>
                {
                    // Se vier o header de dev, usa o bypass; senão, tenta validar como JWT normal.
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.ContainsKey(DevAuthHandler.RoleHeader)
                            ? DevAuthHandler.SchemeName
                            : JwtBearerDefaults.AuthenticationScheme;
                });
        }

        services.AddAuthorization();

        return services;
    }
}
