using FiapEsperancaSolidaria.Campanha.Api.Configurations;
using FiapEsperancaSolidaria.Campanha.Api.Configurations.OpenApi;
using FiapEsperancaSolidaria.Campanha.Application.Configurations;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Configurations;
using FiapEsperancaSolidaria.Campanha.Observability.Configurations;
using FiapEsperancaSolidaria.Campanha.Observability.Middlewares;
using Serilog;
using System.Text.Json.Serialization;
using FiapEsperancaSolidaria.Campanha.Queue.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddObservability();
builder.Services.AddQueueConfig(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthConfig(builder.Configuration, builder.Environment);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApiConfiguration();

builder.Services.AddHealthCheckConfiguration(builder.Configuration);

var app = builder.Build();

app.MigrateDatabase();

app.MapOpenApiConfiguration();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthCheckEndpoints();

app.Run();

public partial class Program;
