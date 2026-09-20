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

    private HttpClient CreateClient(string? role = null, Guid? userId = null)
    {
        var client = _factory.CreateClient();

        if (role is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);

        if (userId is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

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
        Amount = 120m,
        PaymentMethod = "Pix"
    };

    private async Task<CampaignResponse> CreateCampaignAsync(HttpClient managerClient, string? title = null)
    {
        var response = await managerClient.PostAsJsonAsync(
            CampaignRoute,
            CreateValidCampaignPayload(title ?? $"Campaign {Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CampaignResponse>())!;
    }

    [Fact]
    public async Task Create_WithDoadorRole_ShouldReturn201AndPersistDonation()
    {
        var managerClient = CreateClient("GestorONG");
        var donorClient = CreateClient("Doador");
        var campaign = await CreateCampaignAsync(managerClient);

        var response = await donorClient.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(campaign.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<DonationResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.CampaignId.Should().Be(campaign.Id);
        body.DonorId.Should().Be(TestAuthHandler.TestUserId);
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
    public async Task Create_WithGestorONGRole_ShouldReturn403()
    {
        var client = CreateClient("GestorONG");

        var response = await client.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithInvalidPayload_ShouldReturn400()
    {
        var donorClient = CreateClient("Doador");
        var invalidPayload = new
        {
            CampaignId = Guid.NewGuid(),
            Amount = 0m,
            PaymentMethod = "Pix"
        };

        var response = await donorClient.PostAsJsonAsync(BaseRoute, invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WhenCampaignDoesNotExist_ShouldReturn422()
    {
        var donorClient = CreateClient("Doador");
        var payload = CreateValidDonationPayload(Guid.NewGuid());

        var response = await donorClient.PostAsJsonAsync(BaseRoute, payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_WhenOwner_ShouldReturn200WithDonationData()
    {
        var managerClient = CreateClient("GestorONG");
        var donorClient = CreateClient("Doador");
        var campaign = await CreateCampaignAsync(managerClient);

        var created = await donorClient.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(campaign.Id));
        var donation = await created.Content.ReadFromJsonAsync<DonationResponse>(JsonOptions);

        var response = await donorClient.GetAsync($"{BaseRoute}/{donation!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>(JsonOptions);
        body!.Id.Should().Be(donation.Id);
        body.CampaignId.Should().Be(campaign.Id);
    }

    [Fact]
    public async Task GetById_WhenGestorONG_ShouldReturn200EvenNotOwner()
    {
        var managerClient = CreateClient("GestorONG", userId: Guid.NewGuid());
        var donorClient = CreateClient("Doador", userId: Guid.NewGuid());
        var campaign = await CreateCampaignAsync(managerClient);

        var created = await donorClient.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(campaign.Id));
        var donation = await created.Content.ReadFromJsonAsync<DonationResponse>(JsonOptions);

        var response = await managerClient.GetAsync($"{BaseRoute}/{donation!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_WhenNotOwnerAndNotGestorONG_ShouldReturn401()
    {
        var managerClient = CreateClient("GestorONG");
        var donorClient = CreateClient("Doador", userId: Guid.NewGuid());
        var otherDonorClient = CreateClient("Doador", userId: Guid.NewGuid());
        var campaign = await CreateCampaignAsync(managerClient);

        var created = await donorClient.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(campaign.Id));
        var donation = await created.Content.ReadFromJsonAsync<DonationResponse>(JsonOptions);

        var response = await otherDonorClient.GetAsync($"{BaseRoute}/{donation!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WithoutAuthentication_ShouldReturn401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturn404()
    {
        var client = CreateClient("Doador");

        var response = await client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListMine_ShouldIncludeDonationsFromCampaignsNoLongerActive()
    {
        var managerClient = CreateClient("GestorONG");
        var donorClient = CreateClient("Doador");
        var campaign = await CreateCampaignAsync(managerClient);

        var created = await donorClient.PostAsJsonAsync(BaseRoute, CreateValidDonationPayload(campaign.Id));
        var donation = await created.Content.ReadFromJsonAsync<DonationResponse>(JsonOptions);

        var cancelResponse = await managerClient.PostAsync($"{CampaignRoute}/{campaign.Id}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await donorClient.GetAsync($"{BaseRoute}/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipts = await response.Content.ReadFromJsonAsync<List<DonationReceiptResponse>>(JsonOptions);
        receipts.Should().ContainSingle(r => r.DonationId == donation!.Id);

        var receipt = receipts!.Single(r => r.DonationId == donation!.Id);
        receipt.CampaignId.Should().Be(campaign.Id);
        receipt.CampaignTitle.Should().Be(campaign.Title);
        receipt.CampaignStatus.Should().Be("Cancelled");
    }

    [Fact]
    public async Task ListMine_WithoutAuthentication_ShouldReturn401()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"{BaseRoute}/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListMine_WithGestorONGRole_ShouldReturn403()
    {
        var client = CreateClient("GestorONG");

        var response = await client.GetAsync($"{BaseRoute}/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
