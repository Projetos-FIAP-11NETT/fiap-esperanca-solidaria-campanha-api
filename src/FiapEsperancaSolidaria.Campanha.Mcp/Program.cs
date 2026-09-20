using FiapEsperancaSolidaria.Campanha.Mcp;
using FiapEsperancaSolidaria.Campanha.Mcp.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// No transporte stdio o stdout é o canal do protocolo MCP: qualquer log ali corrompe a conversa.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

var settings = McpSettings.From(builder.Configuration);

// Singletons: o DonorSession precisa viver o processo todo pra reaproveitar o token do login.
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(new DonorSession(
    new HttpClient { BaseAddress = McpSettings.AsBaseAddress(settings.UsuarioApiUrl), Timeout = TimeSpan.FromSeconds(30) },
    settings));
builder.Services.AddSingleton(sp => new CampanhaApiClient(
    new HttpClient { BaseAddress = McpSettings.AsBaseAddress(settings.CampanhaApiUrl), Timeout = TimeSpan.FromSeconds(30) },
    sp.GetRequiredService<DonorSession>()));

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
