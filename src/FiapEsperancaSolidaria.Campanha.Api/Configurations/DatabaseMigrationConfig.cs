using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations;

public static class DatabaseMigrationConfig
{
    public static WebApplication MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();

        return app;
    }
}
