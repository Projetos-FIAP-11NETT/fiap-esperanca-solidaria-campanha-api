using System.Net;
using System.Net.Http.Json;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Tests.Integration.TestSupport;
using FluentAssertions;

namespace FiapEsperancaSolidaria.Campanha.Tests.Integration;

public class CampanhaControllerTests : IClassFixture<CampanhaApiFactory>
{
    private const string RotaBase = "/api/v1/campanhas";

    private readonly CampanhaApiFactory _factory;

    public CampanhaControllerTests(CampanhaApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CriarCliente(string? role = null)
    {
        var client = _factory.CreateClient();

        if (role is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);

        return client;
    }

    private static object CriarPayloadValido(string titulo) => new
    {
        Titulo = titulo,
        Descricao = "Descrição de teste",
        DataInicio = DateTime.UtcNow.Date,
        DataFim = DateTime.UtcNow.Date.AddDays(30),
        MetaFinanceira = 1000m,
        Imagem = (string?)null
    };

    [Fact]
    public async Task Criar_ComRoleGestorONG_DeveRetornar201EPersistirCampanha()
    {
        var client = CriarCliente("GestorONG");
        var titulo = $"Campanha {Guid.NewGuid()}";

        var response = await client.PostAsJsonAsync(RotaBase, CriarPayloadValido(titulo));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CampanhaResponse>();
        body.Should().NotBeNull();
        body!.Titulo.Should().Be(titulo);
        body.Status.Should().Be("Ativa");
        body.ValorTotalArrecadado.Should().Be(0);
    }

    [Fact]
    public async Task Criar_SemAutenticacao_DeveRetornar401()
    {
        var client = CriarCliente();

        var response = await client.PostAsJsonAsync(RotaBase, CriarPayloadValido($"Campanha {Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Criar_ComRoleDoador_DeveRetornar403()
    {
        var client = CriarCliente("Doador");

        var response = await client.PostAsJsonAsync(RotaBase, CriarPayloadValido($"Campanha {Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Criar_ComPayloadInvalido_DeveRetornar400()
    {
        var client = CriarCliente("GestorONG");
        var payloadInvalido = new
        {
            Titulo = "",
            Descricao = "Descrição de teste",
            DataInicio = DateTime.UtcNow.Date,
            DataFim = DateTime.UtcNow.Date.AddDays(30),
            MetaFinanceira = 0m,
            Imagem = (string?)null
        };

        var response = await client.PostAsJsonAsync(RotaBase, payloadInvalido);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ComTituloJaExistente_DeveRetornar400()
    {
        var client = CriarCliente("GestorONG");
        var titulo = $"Campanha Duplicada {Guid.NewGuid()}";

        var primeira = await client.PostAsJsonAsync(RotaBase, CriarPayloadValido(titulo));
        primeira.StatusCode.Should().Be(HttpStatusCode.Created);

        var segunda = await client.PostAsJsonAsync(RotaBase, CriarPayloadValido(titulo));

        segunda.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ComTituloExistenteEmOutraCaixa_DeveRetornar400()
    {
        var client = CriarCliente("GestorONG");
        var titulo = $"Campanha Case {Guid.NewGuid()}";

        var primeira = await client.PostAsJsonAsync(RotaBase, CriarPayloadValido(titulo));
        primeira.StatusCode.Should().Be(HttpStatusCode.Created);

        var segunda = await client.PostAsJsonAsync(RotaBase, CriarPayloadValido(titulo.ToUpperInvariant()));

        segunda.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ObterPorId_QuandoExiste_DeveRetornar200SemAutenticacao()
    {
        var clienteGestor = CriarCliente("GestorONG");
        var titulo = $"Campanha {Guid.NewGuid()}";
        var criado = await clienteGestor.PostAsJsonAsync(RotaBase, CriarPayloadValido(titulo));
        var campanhaCriada = await criado.Content.ReadFromJsonAsync<CampanhaResponse>();

        var clienteAnonimo = CriarCliente();
        var response = await clienteAnonimo.GetAsync($"{RotaBase}/{campanhaCriada!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CampanhaResponse>();
        body!.Id.Should().Be(campanhaCriada.Id);
    }

    [Fact]
    public async Task ObterPorId_QuandoNaoExiste_DeveRetornar404()
    {
        var client = CriarCliente();

        var response = await client.GetAsync($"{RotaBase}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListarPublicas_DeveConterCampanhaAtivaRecemCriada()
    {
        var clienteGestor = CriarCliente("GestorONG");
        var titulo = $"Campanha {Guid.NewGuid()}";
        await clienteGestor.PostAsJsonAsync(RotaBase, CriarPayloadValido(titulo));

        var clienteAnonimo = CriarCliente();
        var response = await clienteAnonimo.GetAsync($"{RotaBase}/publicas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await response.Content.ReadFromJsonAsync<List<CampanhaPublicaResponse>>();
        lista.Should().Contain(c => c.Titulo == titulo);
    }

    [Fact]
    public async Task Atualizar_ComRoleGestorONG_DeveAtualizarERefletirNaConsultaPorId()
    {
        var clienteGestor = CriarCliente("GestorONG");
        var criado = await clienteGestor.PostAsJsonAsync(RotaBase, CriarPayloadValido($"Campanha {Guid.NewGuid()}"));
        var campanhaCriada = await criado.Content.ReadFromJsonAsync<CampanhaResponse>();

        var novoTitulo = $"Campanha atualizada {Guid.NewGuid()}";
        var payloadAtualizacao = new
        {
            Titulo = novoTitulo,
            Descricao = "Descrição atualizada",
            DataInicio = DateTime.UtcNow.Date,
            DataFim = DateTime.UtcNow.Date.AddDays(60),
            MetaFinanceira = 2000m,
            Imagem = (string?)null,
            Status = "Ativa"
        };

        var responseAtualizacao = await clienteGestor.PutAsJsonAsync($"{RotaBase}/{campanhaCriada!.Id}", payloadAtualizacao);
        responseAtualizacao.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseConsulta = await CriarCliente().GetAsync($"{RotaBase}/{campanhaCriada.Id}");
        var campanhaAtualizada = await responseConsulta.Content.ReadFromJsonAsync<CampanhaResponse>();

        campanhaAtualizada!.Titulo.Should().Be(novoTitulo);
        campanhaAtualizada.MetaFinanceira.Should().Be(2000m);
    }

    [Fact]
    public async Task Atualizar_QuandoNaoExiste_DeveRetornar404()
    {
        var clienteGestor = CriarCliente("GestorONG");
        var payload = new
        {
            Titulo = "Título",
            Descricao = "Descrição",
            DataInicio = DateTime.UtcNow.Date,
            DataFim = DateTime.UtcNow.Date.AddDays(30),
            MetaFinanceira = 1000m,
            Imagem = (string?)null,
            Status = "Ativa"
        };

        var response = await clienteGestor.PutAsJsonAsync($"{RotaBase}/{Guid.NewGuid()}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Atualizar_SemAutenticacao_DeveRetornar401()
    {
        var client = CriarCliente();
        var payload = new
        {
            Titulo = "Título",
            Descricao = "Descrição",
            DataInicio = DateTime.UtcNow.Date,
            DataFim = DateTime.UtcNow.Date.AddDays(30),
            MetaFinanceira = 1000m,
            Imagem = (string?)null,
            Status = "Ativa"
        };

        var response = await client.PutAsJsonAsync($"{RotaBase}/{Guid.NewGuid()}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FluxoCompleto_AoCancelarCampanha_CacheDeListagemPublicaDeveSerInvalidado()
    {
        var clienteGestor = CriarCliente("GestorONG");
        var titulo = $"Campanha {Guid.NewGuid()}";
        var criado = await clienteGestor.PostAsJsonAsync(RotaBase, CriarPayloadValido(titulo));
        var campanhaCriada = await criado.Content.ReadFromJsonAsync<CampanhaResponse>();

        var clienteAnonimo = CriarCliente();

        // Popula o cache da listagem pública.
        var listaAntes = await (await clienteAnonimo.GetAsync($"{RotaBase}/publicas"))
            .Content.ReadFromJsonAsync<List<CampanhaPublicaResponse>>();
        listaAntes.Should().Contain(c => c.Titulo == titulo);

        var payloadCancelamento = new
        {
            Titulo = titulo,
            Descricao = "Descrição de teste",
            DataInicio = DateTime.UtcNow.Date,
            DataFim = DateTime.UtcNow.Date.AddDays(30),
            MetaFinanceira = 1000m,
            Imagem = (string?)null,
            Status = "Cancelada"
        };
        await clienteGestor.PutAsJsonAsync($"{RotaBase}/{campanhaCriada!.Id}", payloadCancelamento);

        // Se a invalidação de cache não funcionar, a lista abaixo ainda viria da entrada em cache (stale).
        var listaDepois = await (await clienteAnonimo.GetAsync($"{RotaBase}/publicas"))
            .Content.ReadFromJsonAsync<List<CampanhaPublicaResponse>>();
        listaDepois.Should().NotContain(c => c.Id == campanhaCriada.Id);
    }
}
