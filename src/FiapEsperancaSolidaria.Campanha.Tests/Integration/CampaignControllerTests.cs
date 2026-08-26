using System.Net;
using System.Net.Http.Json;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Tests.Integration.TestSupport;
using FluentAssertions;

namespace FiapEsperancaSolidaria.Campanha.Tests.Integration;

public class CampaignControllerTests : IClassFixture<CampaignApiFactory>
{
    private const string BaseRoute = "/api/v1/campaigns";

    private readonly CampaignApiFactory _factory;

    public CampaignControllerTests(CampaignApiFactory factory)
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

    private static object CreateValidPayload(string title) => new
    {
        Title = title,
        Description = "Test description",
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(30),
        FinancialGoal = 1000m,
        Image = (string?)null
    };

    [Fact]
    public async Task Create_WithGestorONGRole_ShouldReturn201AndPersistCampaign()
    {
        var client = CreateClient("GestorONG");
        var title = $"Campaign {Guid.NewGuid()}";

        var response = await client.PostAsJsonAsync(BaseRoute, CreateValidPayload(title));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CampaignResponse>();
        body.Should().NotBeNull();
        body!.Title.Should().Be(title);
        body.Status.Should().Be("Active");
        body.TotalRaised.Should().Be(0);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturn401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(BaseRoute, CreateValidPayload($"Campaign {Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithDoadorRole_ShouldReturn403()
    {
        var client = CreateClient("Doador");

        var response = await client.PostAsJsonAsync(BaseRoute, CreateValidPayload($"Campaign {Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithInvalidPayload_ShouldReturn400()
    {
        var client = CreateClient("GestorONG");
        var invalidPayload = new
        {
            Title = "",
            Description = "Test description",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            FinancialGoal = 0m,
            Image = (string?)null
        };

        var response = await client.PostAsJsonAsync(BaseRoute, invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithExistingTitle_ShouldReturn400()
    {
        var client = CreateClient("GestorONG");
        var title = $"Duplicate Campaign {Guid.NewGuid()}";

        var first = await client.PostAsJsonAsync(BaseRoute, CreateValidPayload(title));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync(BaseRoute, CreateValidPayload(title));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithExistingTitleInDifferentCase_ShouldReturn400()
    {
        var client = CreateClient("GestorONG");
        var title = $"Campaign Case {Guid.NewGuid()}";

        var first = await client.PostAsJsonAsync(BaseRoute, CreateValidPayload(title));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync(BaseRoute, CreateValidPayload(title.ToUpperInvariant()));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WhenExists_ShouldReturn200WithoutAuthentication()
    {
        var managerClient = CreateClient("GestorONG");
        var title = $"Campaign {Guid.NewGuid()}";
        var created = await managerClient.PostAsJsonAsync(BaseRoute, CreateValidPayload(title));
        var createdCampaign = await created.Content.ReadFromJsonAsync<CampaignResponse>();

        var anonymousClient = CreateClient();
        var response = await anonymousClient.GetAsync($"{BaseRoute}/{createdCampaign!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CampaignResponse>();
        body!.Id.Should().Be(createdCampaign.Id);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturn404()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListPublic_ShouldContainRecentlyCreatedActiveCampaign()
    {
        var managerClient = CreateClient("GestorONG");
        var title = $"Campaign {Guid.NewGuid()}";
        await managerClient.PostAsJsonAsync(BaseRoute, CreateValidPayload(title));

        var anonymousClient = CreateClient();
        var response = await anonymousClient.GetAsync($"{BaseRoute}/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<PublicCampaignResponse>>();
        list.Should().Contain(c => c.Title == title);
    }

    [Fact]
    public async Task Update_WithGestorONGRole_ShouldUpdateAndReflectInGetById()
    {
        var managerClient = CreateClient("GestorONG");
        var created = await managerClient.PostAsJsonAsync(BaseRoute, CreateValidPayload($"Campaign {Guid.NewGuid()}"));
        var createdCampaign = await created.Content.ReadFromJsonAsync<CampaignResponse>();

        var newTitle = $"Updated Campaign {Guid.NewGuid()}";
        var updatePayload = new
        {
            Title = newTitle,
            Description = "Updated description",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(60),
            FinancialGoal = 2000m,
            Image = (string?)null,
            Status = "Active"
        };

        var updateResponse = await managerClient.PutAsJsonAsync($"{BaseRoute}/{createdCampaign!.Id}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await CreateClient().GetAsync($"{BaseRoute}/{createdCampaign.Id}");
        var updatedCampaign = await getResponse.Content.ReadFromJsonAsync<CampaignResponse>();

        updatedCampaign!.Title.Should().Be(newTitle);
        updatedCampaign.FinancialGoal.Should().Be(2000m);
    }

    [Fact]
    public async Task Update_WhenNotFound_ShouldReturn404()
    {
        var managerClient = CreateClient("GestorONG");
        var payload = new
        {
            Title = "Title",
            Description = "Description",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            FinancialGoal = 1000m,
            Image = (string?)null,
            Status = "Active"
        };

        var response = await managerClient.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithoutAuthentication_ShouldReturn401()
    {
        var client = CreateClient();
        var payload = new
        {
            Title = "Title",
            Description = "Description",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            FinancialGoal = 1000m,
            Image = (string?)null,
            Status = "Active"
        };

        var response = await client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullFlow_WhenCampaignIsCancelled_PublicListingCacheShouldBeInvalidated()
    {
        var managerClient = CreateClient("GestorONG");
        var title = $"Campaign {Guid.NewGuid()}";
        var created = await managerClient.PostAsJsonAsync(BaseRoute, CreateValidPayload(title));
        var createdCampaign = await created.Content.ReadFromJsonAsync<CampaignResponse>();

        var anonymousClient = CreateClient();

        // Popula o cache da listagem pública.
        var listBefore = await (await anonymousClient.GetAsync($"{BaseRoute}/public"))
            .Content.ReadFromJsonAsync<List<PublicCampaignResponse>>();
        listBefore.Should().Contain(c => c.Title == title);

        var cancelPayload = new
        {
            Title = title,
            Description = "Test description",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            FinancialGoal = 1000m,
            Image = (string?)null,
            Status = "Cancelled"
        };
        await managerClient.PutAsJsonAsync($"{BaseRoute}/{createdCampaign!.Id}", cancelPayload);

        // Se a invalidação de cache não funcionar, a lista abaixo ainda viria da entrada em cache (stale).
        var listAfter = await (await anonymousClient.GetAsync($"{BaseRoute}/public"))
            .Content.ReadFromJsonAsync<List<PublicCampaignResponse>>();
        listAfter.Should().NotContain(c => c.Id == createdCampaign.Id);
    }
}
