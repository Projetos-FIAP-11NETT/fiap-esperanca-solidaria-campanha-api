using FiapEsperancaSolidaria.Campanha.Api.Configurations;
using FiapEsperancaSolidaria.Campanha.Application.Configurations;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Configurations;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using FiapEsperancaSolidaria.Campanha.Observability.Configurations;
using FiapEsperancaSolidaria.Campanha.Observability.Middlewares;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddObservability();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthConfig(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig();

builder.Services
    .AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres");

var app = builder.Build();

app.MigrateDatabase();

app.UseSwaggerConfig();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
