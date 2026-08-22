using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.DonationAggregate;
using Microsoft.EntityFrameworkCore;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Entities.Campanha> Campanhas => Set<Domain.Entities.Campanha>();
    public DbSet<Donation> Donations => Set<Donation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
