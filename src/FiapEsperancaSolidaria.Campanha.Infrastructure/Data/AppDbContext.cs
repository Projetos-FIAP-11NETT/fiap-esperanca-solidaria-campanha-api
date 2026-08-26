using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.DonationAggregate;
using Microsoft.EntityFrameworkCore;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Entities.Campaign> Campaigns => Set<Domain.Entities.Campaign>();
    public DbSet<Donation> Donations => Set<Donation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("fundraising");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
