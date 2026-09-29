using FiapEsperancaSolidaria.Campanha.Mcp;
using FiapEsperancaSolidaria.Campanha.Mcp.Api;
using FiapEsperancaSolidaria.Campanha.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// No transporte stdio o stdout é o canal do protocolo MCP: qualquer log ali corrompe a conversa.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

var settings = McpSettings.From(builder.Configuration);

// Singletons: as sessões precisam viver o processo todo pra reaproveitar o token do login.
var usuarioApi = new HttpClient { BaseAddress = McpSettings.AsBaseAddress(settings.UsuarioApiUrl), Timeout = TimeSpan.FromSeconds(30) };
var donor = new AccountSession(usuarioApi, settings.DonorEmail, settings.DonorPassword, "DONOR_EMAIL e DONOR_PASSWORD", "Doador");
var manager = new AccountSession(usuarioApi, settings.ManagerEmail, settings.ManagerPassword, "MANAGER_EMAIL e MANAGER_PASSWORD", "GestorONG");

builder.Services.AddSingleton(new CampanhaApiClient(
    new HttpClient { BaseAddress = McpSettings.AsBaseAddress(settings.CampanhaApiUrl), Timeout = TimeSpan.FromSeconds(30) },
    donor,
    manager));

var mcp = builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<CampaignTools>()
    .WithTools<DonorTools>();

// Ferramentas de admin só existem pra quem configurou a conta de gestor.
if (settings.ManagerConfigured)
    mcp.WithTools<ManagerTools>();

await builder.Build().RunAsync();
