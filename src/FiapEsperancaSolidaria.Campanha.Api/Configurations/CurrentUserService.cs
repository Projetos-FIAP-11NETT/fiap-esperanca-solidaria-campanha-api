using System.Security.Claims;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Identity;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private const string UserIdClaimType = "system_user_id";

    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(UserIdClaimType);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
}
