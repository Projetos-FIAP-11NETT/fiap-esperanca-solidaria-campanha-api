namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Identity;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsInRole(string role);
}
