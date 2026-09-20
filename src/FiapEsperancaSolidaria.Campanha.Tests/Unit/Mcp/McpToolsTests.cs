using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiapEsperancaSolidaria.Campanha.Mcp;
using FiapEsperancaSolidaria.Campanha.Mcp.Api;
using FiapEsperancaSolidaria.Campanha.Mcp.Tools;
using FluentAssertions;
using ModelContextProtocol;

namespace FiapEsperancaSolidaria.Campanha.Tests.Unit.Mcp;

public class McpToolsTests
{
    private static readonly Guid CampaignId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static McpSettings Settings(bool withCredentials = true) => new()
    {
        CampanhaApiUrl = "http://campanha.test",
        UsuarioApiUrl = "http://usuario.test",
        DonorEmail = withCredentials ? "doador@teste.com" : null,
        DonorPassword = withCredentials ? "Senha@123" : null
    };

    private static (CampanhaApiClient Api, DonorSession Session, StubHandler Usuario, StubHandler Campanha) Create(
        Func<HttpRequestMessage, string, HttpResponseMessage> campanhaResponder,
        bool withCredentials = true,
        Func<HttpRequestMessage, string, HttpResponseMessage>? usuarioResponder = null)
    {
        var settings = Settings(withCredentials);
        var usuario = new StubHandler(usuarioResponder ?? ((_, _) => Json(HttpStatusCode.OK, new { idToken = "token-1", expiresIn = 3600 })));
        var campanha = new StubHandler(campanhaResponder);

        var session = new DonorSession(new HttpClient(usuario) { BaseAddress = McpSettings.AsBaseAddress(settings.UsuarioApiUrl) }, settings);
        var api = new CampanhaApiClient(new HttpClient(campanha) { BaseAddress = McpSettings.AsBaseAddress(settings.CampanhaApiUrl) }, session);
        return (api, session, usuario, campanha);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        new(status) { Content = JsonContent.Create(body) };

    private static object ActiveCampaign(string status = "Active") => new
    {
        id = CampaignId,
        title = "Doação para tetraplégicos",
        description = "desc",
        startDate = DateTime.UtcNow,
        endDate = DateTime.UtcNow.AddDays(30),
        image = (string?)null,
        financialGoal = 1000m,
        status,
        totalRaised = 250m
    };

    private static HttpResponseMessage CampaignThenDonation(HttpRequestMessage request, string _) =>
        request.Method == HttpMethod.Get
            ? Json(HttpStatusCode.OK, ActiveCampaign())
            : Json(HttpStatusCode.Created, new
            {
                id = Guid.NewGuid(),
                campaignId = CampaignId,
                donorId = Guid.NewGuid(),
                amount = 50m,
                paymentMethod = "Pix",
                status = "Pending",
                createdAt = DateTime.UtcNow
            });

    [Fact]
    public async Task Donate_WithoutConfirm_ShouldReturnPreviewAndNotCreateDonation()
    {
        var (api, _, usuario, campanha) = Create(CampaignThenDonation);

        var result = await new DonorTools(api).Donate(CampaignId, 50m, "pix", confirm: false);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("donated").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("needsConfirmation").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("preview").GetProperty("paymentMethod").GetString().Should().Be("Pix");
        campanha.Requests.Should().OnlyContain(r => r.Method == HttpMethod.Get);
        usuario.Requests.Should().BeEmpty("a prévia não precisa de login");
    }

    [Fact]
    public async Task Donate_WithConfirm_ShouldPostWithBearerTokenAndNoDonorIdInBody()
    {
        var (api, _, _, campanha) = Create(CampaignThenDonation);

        var result = await new DonorTools(api).Donate(CampaignId, 50m, "PIX", confirm: true);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("donated").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("donation").GetProperty("status").GetString().Should().Be("Pending");

        var post = campanha.Requests.Single(r => r.Method == HttpMethod.Post);
        post.Path.Should().Be("/api/v1/Donation");
        post.Authorization.Should().Be("Bearer token-1");

        using var body = JsonDocument.Parse(post.Body!);
        body.RootElement.GetProperty("campaignId").GetGuid().Should().Be(CampaignId);
        body.RootElement.GetProperty("amount").GetDecimal().Should().Be(50m);
        body.RootElement.GetProperty("paymentMethod").GetString().Should().Be("Pix");
        body.RootElement.TryGetProperty("donorId", out _).Should().BeFalse("o doador vem do token, não do corpo");
    }

    [Fact]
    public async Task Donate_WithInvalidPaymentMethod_ShouldThrowWithoutCallingApi()
    {
        var (api, _, _, campanha) = Create(CampaignThenDonation);

        var act = () => new DonorTools(api).Donate(CampaignId, 50m, "Bitcoin", confirm: true);

        (await act.Should().ThrowAsync<McpException>()).WithMessage("*Bitcoin*CreditCard*");
        campanha.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Donate_WithNonPositiveAmount_ShouldThrowWithoutCallingApi(decimal amount)
    {
        var (api, _, _, campanha) = Create(CampaignThenDonation);

        var act = () => new DonorTools(api).Donate(CampaignId, amount, "Pix", confirm: true);

        await act.Should().ThrowAsync<McpException>();
        campanha.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Donate_WhenCampaignIsNotActive_ShouldThrowAndNotCreateDonation()
    {
        var (api, _, _, campanha) = Create((_, _) => Json(HttpStatusCode.OK, ActiveCampaign("Cancelled")));

        var act = () => new DonorTools(api).Donate(CampaignId, 50m, "Pix", confirm: true);

        (await act.Should().ThrowAsync<McpException>()).WithMessage("*não está ativa*Cancelled*");
        campanha.Requests.Should().OnlyContain(r => r.Method == HttpMethod.Get);
    }

    [Fact]
    public async Task MyDonations_ShouldSummarizeTotalsAndCampaignsNoLongerActive()
    {
        var other = Guid.NewGuid();
        var receipts = new object[]
        {
            Receipt(CampaignId, "Active", "Approved", 100m),
            Receipt(CampaignId, "Active", "Pending", 40m),
            Receipt(other, "Cancelled", "Approved", 60m),
            Receipt(other, "Cancelled", "Rejected", 10m)
        };
        var (api, _, _, _) = Create((_, _) => Json(HttpStatusCode.OK, receipts));

        var result = await new DonorTools(api).MyDonations();

        using var doc = JsonDocument.Parse(result);
        var summary = doc.RootElement.GetProperty("summary");
        summary.GetProperty("donations").GetInt32().Should().Be(4);
        summary.GetProperty("campaignsSupported").GetInt32().Should().Be(2);
        summary.GetProperty("campaignsNoLongerActive").GetInt32().Should().Be(1);
        summary.GetProperty("totalApproved").GetDecimal().Should().Be(160m);
        summary.GetProperty("totalInProgress").GetDecimal().Should().Be(40m);
        summary.GetProperty("totalRejected").GetDecimal().Should().Be(10m);
        doc.RootElement.GetProperty("donations").GetArrayLength().Should().Be(4);
    }

    [Fact]
    public async Task Session_ShouldLoginOnlyOnceAcrossCalls()
    {
        var (api, _, usuario, _) = Create((_, _) => Json(HttpStatusCode.OK, Array.Empty<object>()));
        var tools = new DonorTools(api);

        await tools.MyDonations();
        await tools.MyDonations();

        usuario.Requests.Should().ContainSingle();
        usuario.Requests[0].Path.Should().Be("/api/v1/User/Login");
    }

    [Fact]
    public async Task Session_WithoutCredentials_ShouldThrowClearMessage()
    {
        var (api, _, usuario, campanha) = Create((_, _) => Json(HttpStatusCode.OK, Array.Empty<object>()), withCredentials: false);

        var act = () => new DonorTools(api).MyDonations();

        (await act.Should().ThrowAsync<McpException>()).WithMessage("*DONOR_EMAIL*DONOR_PASSWORD*");
        usuario.Requests.Should().BeEmpty();
        campanha.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Session_WhenLoginFails_ShouldThrowWithoutLeakingPassword()
    {
        var (api, _, _, _) = Create(
            (_, _) => Json(HttpStatusCode.OK, Array.Empty<object>()),
            usuarioResponder: (_, _) => Json(HttpStatusCode.Unauthorized, new { }));

        var act = () => new DonorTools(api).MyDonations();

        var error = (await act.Should().ThrowAsync<McpException>()).Which;
        error.Message.Should().Contain("Login na usuario-api falhou");
        error.Message.Should().NotContain("Senha@123");
    }

    [Fact]
    public async Task Client_WhenTokenIsRejected_ShouldLoginAgainAndRetryOnce()
    {
        var calls = 0;
        var (api, _, usuario, campanha) = Create((_, _) =>
            ++calls == 1
                ? Json(HttpStatusCode.Unauthorized, new { })
                : Json(HttpStatusCode.OK, Array.Empty<object>()));

        var result = await new DonorTools(api).MyDonations();

        result.Should().Contain("\"donations\":0");
        usuario.Requests.Should().HaveCount(2, "o token descartado exige um login novo");
        campanha.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Client_ShouldSurfaceApiErrorMessage()
    {
        var (api, _, _, _) = Create((_, _) => Json(HttpStatusCode.NotFound, new { error = "Campanha 'x' não encontrada." }));

        var act = () => new CampaignTools(api).GetCampaign(CampaignId);

        (await act.Should().ThrowAsync<McpException>()).WithMessage("Campanha 'x' não encontrada.");
    }

    [Fact]
    public async Task SearchCampaigns_ShouldEscapeTitleAndComputeProgress()
    {
        var campaigns = new[]
        {
            new { id = CampaignId, title = "Campanha A", description = "d", image = (string?)null, financialGoal = 1000m, totalRaised = 250m }
        };
        var (api, _, usuario, campanha) = Create((_, _) => Json(HttpStatusCode.OK, campaigns));

        var result = await new CampaignTools(api).SearchCampaigns("tetra & plégicos");

        campanha.Requests.Single().PathAndQuery.Should().Be("/api/v1/Campaign/public?title=tetra%20%26%20pl%C3%A9gicos");
        campanha.Requests.Single().Authorization.Should().BeNull("busca pública não usa login");
        usuario.Requests.Should().BeEmpty();

        using var doc = JsonDocument.Parse(result);
        var campaign = doc.RootElement.GetProperty("campaigns")[0];
        campaign.GetProperty("percentReached").GetDecimal().Should().Be(25m);
        campaign.GetProperty("remaining").GetDecimal().Should().Be(750m);
    }

    [Fact]
    public async Task TransparencySummary_ShouldAggregateAndListClosestToGoal()
    {
        var campaigns = new object[]
        {
            new { id = Guid.NewGuid(), title = "Longe", description = "d", image = (string?)null, financialGoal = 1000m, totalRaised = 100m },
            new { id = Guid.NewGuid(), title = "Perto", description = "d", image = (string?)null, financialGoal = 1000m, totalRaised = 900m },
            new { id = Guid.NewGuid(), title = "Batida", description = "d", image = (string?)null, financialGoal = 500m, totalRaised = 500m }
        };
        var (api, _, _, _) = Create((_, _) => Json(HttpStatusCode.OK, campaigns));

        var result = await new CampaignTools(api).TransparencySummary();

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("activeCampaigns").GetInt32().Should().Be(3);
        doc.RootElement.GetProperty("totalGoal").GetDecimal().Should().Be(2500m);
        doc.RootElement.GetProperty("totalRaised").GetDecimal().Should().Be(1500m);
        doc.RootElement.GetProperty("percentReached").GetDecimal().Should().Be(60m);

        var closest = doc.RootElement.GetProperty("closestToGoal");
        closest.GetArrayLength().Should().Be(2, "campanha que já bateu a meta não entra");
        closest[0].GetProperty("title").GetString().Should().Be("Perto");
    }

    private static object Receipt(Guid campaignId, string campaignStatus, string status, decimal amount) => new
    {
        donationId = Guid.NewGuid(),
        campaignId,
        campaignTitle = "Campanha",
        campaignStatus,
        amount,
        paymentMethod = "Pix",
        status,
        createdAt = DateTime.UtcNow
    };

    private sealed record CapturedRequest(HttpMethod Method, string PathAndQuery, string? Authorization, string? Body)
    {
        public string Path => PathAndQuery.Split('?')[0];
    }

    private sealed class StubHandler(Func<HttpRequestMessage, string, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri!.PathAndQuery,
                request.Headers.Authorization?.ToString(),
                body));

            return responder(request, body ?? string.Empty);
        }
    }
}
