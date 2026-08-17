using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Data;

/// <summary>
/// Usado apenas em design-time pelas ferramentas do EF Core (dotnet ef migrations),
/// já que nesse momento não existe host da Api para fornecer a connection string real.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5444;Database=campanha-db;Username=postgres;Password=postgres;");

        return new AppDbContext(optionsBuilder.Options);
    }
}
