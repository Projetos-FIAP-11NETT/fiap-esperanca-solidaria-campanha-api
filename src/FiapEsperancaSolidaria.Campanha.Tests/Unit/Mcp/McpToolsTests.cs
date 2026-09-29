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
        DonorPassword = withCredentials ? "Senha@123" : null,
        ManagerEmail = withCredentials ? "gestor@teste.com" : null,
        ManagerPassword = withCredentials ? "Senha@456" : null
    };

    // O login falso devolve um token que carrega o e-mail, pra os testes provarem qual conta
    // (doador ou gestor) fez cada chamada.
    private static HttpResponseMessage DefaultLogin(HttpRequestMessage _, string body)
    {
        using var doc = JsonDocument.Parse(body);
        var email = doc.RootElement.GetProperty("email").GetString();
        return Json(HttpStatusCode.OK, new { idToken = $"token-{email}", expiresIn = 3600 });
    }

    private static (CampanhaApiClient Api, StubHandler Usuario, StubHandler Campanha) Create(
        Func<HttpRequestMessage, string, HttpResponseMessage> campanhaResponder,
        bool withCredentials = true,
        Func<HttpRequestMessage, string, HttpResponseMessage>? usuarioResponder = null)
    {
        var settings = Settings(withCredentials);
        var usuario = new StubHandler(usuarioResponder ?? DefaultLogin);
        var campanha = new StubHandler(campanhaResponder);

        var usuarioHttp = new HttpClient(usuario) { BaseAddress = McpSettings.AsBaseAddress(settings.UsuarioApiUrl) };
        var donor = new AccountSession(usuarioHttp, settings.DonorEmail, settings.DonorPassword, "DONOR_EMAIL e DONOR_PASSWORD", "Doador");
        var manager = new AccountSession(usuarioHttp, settings.ManagerEmail, settings.ManagerPassword, "MANAGER_EMAIL e MANAGER_PASSWORD", "GestorONG");

        var api = new CampanhaApiClient(
            new HttpClient(campanha) { BaseAddress = McpSettings.AsBaseAddress(settings.CampanhaApiUrl) },
            donor,
            manager);
        return (api, usuario, campanha);
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
        var (api, usuario, campanha) = Create(CampaignThenDonation);

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
        var (api, _, campanha) = Create(CampaignThenDonation);

        var result = await new DonorTools(api).Donate(CampaignId, 50m, "PIX", confirm: true);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("donated").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("donation").GetProperty("status").GetString().Should().Be("Pending");

        var post = campanha.Requests.Single(r => r.Method == HttpMethod.Post);
        post.Path.Should().Be("/api/v1/Donation");
        post.Authorization.Should().Be("Bearer token-doador@teste.com");

        using var body = JsonDocument.Parse(post.Body!);
        body.RootElement.GetProperty("campaignId").GetGuid().Should().Be(CampaignId);
        body.RootElement.GetProperty("amount").GetDecimal().Should().Be(50m);
        body.RootElement.GetProperty("paymentMethod").GetString().Should().Be("Pix");
        body.RootElement.TryGetProperty("donorId", out _).Should().BeFalse("o doador vem do token, não do corpo");
    }

    [Fact]
    public async Task Donate_WithInvalidPaymentMethod_ShouldThrowWithoutCallingApi()
    {
        var (api, _, campanha) = Create(CampaignThenDonation);

        var act = () => new DonorTools(api).Donate(CampaignId, 50m, "Bitcoin", confirm: true);

        (await act.Should().ThrowAsync<McpException>()).WithMessage("*Bitcoin*CreditCard*");
        campanha.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Donate_WithNonPositiveAmount_ShouldThrowWithoutCallingApi(decimal amount)
    {
        var (api, _, campanha) = Create(CampaignThenDonation);

        var act = () => new DonorTools(api).Donate(CampaignId, amount, "Pix", confirm: true);

        await act.Should().ThrowAsync<McpException>();
        campanha.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Donate_WhenCampaignIsNotActive_ShouldThrowAndNotCreateDonation()
    {
        var (api, _, campanha) = Create((_, _) => Json(HttpStatusCode.OK, ActiveCampaign("Cancelled")));

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
        var (api, _, _) = Create((_, _) => Json(HttpStatusCode.OK, receipts));

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
        var (api, usuario, _) = Create((_, _) => Json(HttpStatusCode.OK, Array.Empty<object>()));
        var tools = new DonorTools(api);

        await tools.MyDonations();
        await tools.MyDonations();

        usuario.Requests.Should().ContainSingle();
        usuario.Requests[0].Path.Should().Be("/api/v1/User/Login");
    }

    [Fact]
    public async Task Session_WithoutCredentials_ShouldThrowClearMessage()
    {
        var (api, usuario, campanha) = Create((_, _) => Json(HttpStatusCode.OK, Array.Empty<object>()), withCredentials: false);

        var act = () => new DonorTools(api).MyDonations();

        (await act.Should().ThrowAsync<McpException>()).WithMessage("*DONOR_EMAIL*DONOR_PASSWORD*");
        usuario.Requests.Should().BeEmpty();
        campanha.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Session_WhenLoginFails_ShouldThrowWithoutLeakingPassword()
    {
        var (api, _, _) = Create(
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
        var (api, usuario, campanha) = Create((_, _) =>
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
        var (api, _, _) = Create((_, _) => Json(HttpStatusCode.NotFound, new { error = "Campanha 'x' não encontrada." }));

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
        var (api, usuario, campanha) = Create((_, _) => Json(HttpStatusCode.OK, campaigns));

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
        var (api, _, _) = Create((_, _) => Json(HttpStatusCode.OK, campaigns));

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

    // --- tools do gestor ---

    private static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");
    private static string InDays(int days) => DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-dd");

    private static object ManagedCampaign(string status, decimal goal = 1000m, decimal raised = 250m) => new
    {
        id = CampaignId,
        title = "Cestas básicas",
        description = "desc",
        startDate = DateTime.UtcNow,
        endDate = DateTime.UtcNow.AddDays(30),
        image = (string?)null,
        financialGoal = goal,
        status,
        totalRaised = raised
    };

    [Fact]
    public async Task ListAllCampaigns_ShouldCountByStatusAndFilter()
    {
        var campaigns = new[] { ManagedCampaign("Active"), ManagedCampaign("Active"), ManagedCampaign("Cancelled"), ManagedCampaign("Scheduled") };
        var (api, _, campanha) = Create((_, _) => Json(HttpStatusCode.OK, campaigns));

        var result = await new ManagerTools(api).ListAllCampaigns("active");

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("total").GetInt32().Should().Be(4);
        doc.RootElement.GetProperty("shown").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("byStatus").GetProperty("Cancelled").GetInt32().Should().Be(1);
        campanha.Requests.Single().Authorization.Should().Be("Bearer token-gestor@teste.com");
        campanha.Requests.Single().Path.Should().Be("/api/v1/Campaign");
    }

    [Fact]
    public async Task ListAllCampaigns_WithInvalidStatus_ShouldThrowWithoutCallingApi()
    {
        var (api, _, campanha) = Create((_, _) => Json(HttpStatusCode.OK, Array.Empty<object>()));

        var act = () => new ManagerTools(api).ListAllCampaigns("Pausada");

        (await act.Should().ThrowAsync<McpException>()).WithMessage("*Pausada*Scheduled*");
        campanha.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCampaign_WithoutConfirm_ShouldOnlyPreviewAndExpectScheduledForFutureStart()
    {
        var (api, usuario, campanha) = Create((_, _) => Json(HttpStatusCode.Created, ManagedCampaign("Scheduled")));

        var result = await new ManagerTools(api).CreateCampaign("Cestas", "desc", InDays(7), InDays(37), 5000m, confirm: false);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("created").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("needsConfirmation").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("todayUtc").GetString().Should().Be(Today);
        doc.RootElement.GetProperty("preview").GetProperty("expectedStatus").GetString().Should().Be("Scheduled");
        campanha.Requests.Should().BeEmpty();
        usuario.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCampaign_WithStartToday_ShouldExpectActive()
    {
        var (api, _, _) = Create((_, _) => Json(HttpStatusCode.Created, ManagedCampaign("Active")));

        var result = await new ManagerTools(api).CreateCampaign("Cestas", "desc", Today, InDays(30), 5000m);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("preview").GetProperty("expectedStatus").GetString().Should().Be("Active");
    }

    [Fact]
    public async Task CreateCampaign_WithConfirm_ShouldPostAsManagerWithIsoDates()
    {
        var (api, _, campanha) = Create((_, _) => Json(HttpStatusCode.Created, ManagedCampaign("Scheduled")));

        var result = await new ManagerTools(api).CreateCampaign("  Cestas  ", "desc", InDays(7), InDays(37), 5000m, confirm: true);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("created").GetBoolean().Should().BeTrue();

        var post = campanha.Requests.Single();
        post.Method.Should().Be(HttpMethod.Post);
        post.Path.Should().Be("/api/v1/Campaign");
        post.Authorization.Should().Be("Bearer token-gestor@teste.com", "quem cria é a conta de gestor, não a de doador");

        using var body = JsonDocument.Parse(post.Body!);
        body.RootElement.GetProperty("title").GetString().Should().Be("Cestas");
        body.RootElement.GetProperty("startDate").GetString().Should().Be(InDays(7));
        body.RootElement.GetProperty("endDate").GetString().Should().Be(InDays(37));
        body.RootElement.GetProperty("financialGoal").GetDecimal().Should().Be(5000m);
    }

    [Theory]
    [InlineData("01/10/2026", "2026-10-30", 1000, "*AAAA-MM-DD*")]
    [InlineData("2026-10-30", "2026-10-01", 1000, "*anterior*")]
    [InlineData("2020-01-01", "2020-02-01", 1000, "*passado*")]
    [InlineData("2099-01-01", "2099-02-01", 0, "*meta*")]
    public async Task CreateCampaign_WithInvalidInput_ShouldThrowWithoutCallingApi(string start, string end, decimal goal, string expectedMessage)
    {
        var (api, _, campanha) = Create((_, _) => Json(HttpStatusCode.Created, ManagedCampaign("Active")));

        var act = () => new ManagerTools(api).CreateCampaign("Cestas", "desc", start, end, goal, confirm: true);

        (await act.Should().ThrowAsync<McpException>()).WithMessage(expectedMessage);
        campanha.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelCampaign_WithoutConfirm_WhenGoalNotReached_ShouldPreviewCancelledAndNotPost()
    {
        var (api, _, campanha) = Create((_, _) => Json(HttpStatusCode.OK, ManagedCampaign("Active", goal: 1000m, raised: 250m)));

        var result = await new ManagerTools(api).CancelCampaign(CampaignId, confirm: false);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("cancelled").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("preview").GetProperty("expectedStatusAfter").GetString().Should().Be("Cancelled");
        campanha.Requests.Should().OnlyContain(r => r.Method == HttpMethod.Get);
    }

    [Fact]
    public async Task CancelCampaign_WithoutConfirm_WhenGoalReached_ShouldPreviewCompletedInstead()
    {
        var (api, _, _) = Create((_, _) => Json(HttpStatusCode.OK, ManagedCampaign("Active", goal: 1000m, raised: 1000m)));

        var result = await new ManagerTools(api).CancelCampaign(CampaignId, confirm: false);

        using var doc = JsonDocument.Parse(result);
        var preview = doc.RootElement.GetProperty("preview");
        preview.GetProperty("expectedStatusAfter").GetString().Should().Be("Completed");
        preview.GetProperty("note").GetString().Should().Contain("Completed");
    }

    [Fact]
    public async Task CancelCampaign_WithConfirm_ShouldPostToCancelAsManager()
    {
        var (api, _, campanha) = Create((request, _) => request.Method == HttpMethod.Get
            ? Json(HttpStatusCode.OK, ManagedCampaign("Active"))
            : Json(HttpStatusCode.OK, ManagedCampaign("Cancelled")));

        var result = await new ManagerTools(api).CancelCampaign(CampaignId, confirm: true);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("done").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("status").GetString().Should().Be("Cancelled");

        var post = campanha.Requests.Single(r => r.Method == HttpMethod.Post);
        post.Path.Should().Be($"/api/v1/Campaign/{CampaignId}/cancel");
        post.Authorization.Should().Be("Bearer token-gestor@teste.com");
    }

    [Theory]
    [InlineData("Completed", "*já foi concluída*")]
    [InlineData("Cancelled", "*já está cancelada*")]
    public async Task CancelCampaign_WhenAlreadyFinished_ShouldThrowAndNotPost(string status, string expectedMessage)
    {
        var (api, _, campanha) = Create((_, _) => Json(HttpStatusCode.OK, ManagedCampaign(status)));

        var act = () => new ManagerTools(api).CancelCampaign(CampaignId, confirm: true);

        (await act.Should().ThrowAsync<McpException>()).WithMessage(expectedMessage);
        campanha.Requests.Should().OnlyContain(r => r.Method == HttpMethod.Get);
    }

    [Fact]
    public async Task ManagerTools_WithoutManagerCredentials_ShouldPointToManagerVariables()
    {
        var (api, usuario, _) = Create((_, _) => Json(HttpStatusCode.OK, Array.Empty<object>()), withCredentials: false);

        var act = () => new ManagerTools(api).ListAllCampaigns();

        (await act.Should().ThrowAsync<McpException>()).WithMessage("*MANAGER_EMAIL*MANAGER_PASSWORD*");
        usuario.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ManagerTools_WhenApiForbids_ShouldExplainRequiredRole()
    {
        var (api, _, _) = Create((_, _) => Json(HttpStatusCode.Forbidden, new { }));

        var act = () => new ManagerTools(api).ListAllCampaigns();

        (await act.Should().ThrowAsync<McpException>()).WithMessage("*GestorONG*");
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
