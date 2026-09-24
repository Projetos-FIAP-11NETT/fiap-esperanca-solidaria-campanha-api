using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations;

/// <summary>
/// Bypass de autenticação só para desenvolvimento local, enquanto a usuarios-api (emissora
/// dos tokens Firebase) ainda não existe. Só é registrado quando Auth:DevBypassEnabled=true
/// e o ambiente é Development (ver AuthConfig.AddAuthConfig) — nunca fica ativo em produção.
/// </summary>
public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Dev";
    public const string RoleHeader = "X-Dev-Role";
    public const string UserHeader = "X-Dev-User";

    private static readonly Guid DefaultDevUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrWhiteSpace(role))
            return Task.FromResult(AuthenticateResult.Fail($"Header '{RoleHeader}' é obrigatório no modo dev (ex.: GestorONG ou Doador)."));

        var user = Request.Headers.TryGetValue(UserHeader, out var userHeader) && !string.IsNullOrWhiteSpace(userHeader)
            ? userHeader.ToString()
            : "dev-user";

        var systemUserId = Guid.TryParse(user, out var parsedUserId) ? parsedUserId : DefaultDevUserId;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user),
            new Claim("system_user_id", systemUserId.ToString()),
            new Claim("roles", role.ToString())
        };

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.NameIdentifier, "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        Logger.LogWarning(
            "AUTENTICAÇÃO EM MODO DEV — bypass do Firebase ativo, role '{Role}' aceita sem validação de token.",
            role.ToString());

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
