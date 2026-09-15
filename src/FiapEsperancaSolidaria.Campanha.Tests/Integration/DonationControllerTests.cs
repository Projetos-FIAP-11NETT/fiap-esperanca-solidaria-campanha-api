using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Notifications;
using FiapEsperancaSolidaria.Campanha.Tests.Integration.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiapEsperancaSolidaria.Campanha.Tests.Integration;

public class DonationControllerTests : IClassFixture<DonationApiFactory>
{
    private const string BaseRoute = "/api/v1/donation";
    private const string CampaignRoute = "/api/v1/campaign";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DonationApiFactory _factory;

    public DonationControllerTests(DonationApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(string? role = null)
    {
        var client = _factory.CreateClient();

        if (role is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);

        return client;
    }

    private static object CreateValidCampaignPayload(string title) => new
    {
        Title = title,
        Description = "Test description",
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(30),
        FinancialGoal = 1000m,
        Image = (string?)null
    };

    private static object CreateValidDonationPayload(Guid campaignId) => new
    {
        CampaignId = campaignId,
        DonorId = Guid.NewGuid(),
        Amount = 120m,
        PaymentMethod = "Pix"
    };

    [Fact]
    public async Task Create_WithGestorONGRole_ShouldReturn201AndPersistDonation()
    {
        var managerClient = CreateClient("GestorONG");
        var campaignTitle = $"Campaign {Guid.NewGuid()}";

        var campaignCreateResponse = await managerClient.PostAsJsonAsync(
            CampaignRoute,
            CreateValidCampaignPayload(campaignTitle));

        campaignCreateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var campaign = await campaignCreateResponse.Content.ReadFromJsonAsync<CampaignResponse>();

        var response = await managerClient.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(campaign!.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<DonationResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.CampaignId.Should().Be(campaign.Id);
        body.Amount.Should().Be(120m);
        body.PaymentMethod.ToString().Should().Be("Pix");
        body.Status.ToString().Should().Be("Pending");
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturn401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithDoadorRole_ShouldReturn403()
    {
        var client = CreateClient("Doador");

        var response = await client.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithInvalidPayload_ShouldReturn400()
    {
        var managerClient = CreateClient("GestorONG");
        var invalidPayload = new
        {
            CampaignId = Guid.NewGuid(),
            DonorId = Guid.NewGuid(),
            Amount = 0m,
            PaymentMethod = "Pix"
        };

        var response = await managerClient.PostAsJsonAsync(BaseRoute, invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WhenCampaignDoesNotExist_ShouldReturn422()
    {
        var managerClient = CreateClient("GestorONG");
        var payload = CreateValidDonationPayload(Guid.NewGuid());

        var response = await managerClient.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_WhenExists_ShouldReturn200WithoutAuthentication()
    {
        var managerClient = CreateClient("GestorONG");
        var title = $"Campaign {Guid.NewGuid()}";

        var created = await managerClient.PostAsJsonAsync(CampaignRoute, CreateValidCampaignPayload(title));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCampaign = await created.Content.ReadFromJsonAsync<CampaignResponse>();

        var anonymousClient = CreateClient();
        var response = await anonymousClient.GetAsync($"{BaseRoute}/{createdCampaign!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CampaignResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(createdCampaign.Id);
        body.Title.Should().Be(title);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturn404()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class DonationApiFactory : CampaignApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDonationCreatedNotification>();
            services.AddScoped<IDonationCreatedNotification, NoOpDonationCreatedNotification>();
        });
    }
}

public class NoOpDonationCreatedNotification : IDonationCreatedNotification
{
    public Task PublishAsync(Guid donationId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}