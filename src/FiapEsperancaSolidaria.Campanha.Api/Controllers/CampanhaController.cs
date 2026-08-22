using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.AtualizarCampanha;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Commands.CriarCampanha;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ListarCampanhasPublicas;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ObterCampanhaPorId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapEsperancaSolidaria.Campanha.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CampanhaController : ControllerBase
{
    private readonly IMediator _mediator;

    public CampanhaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Painel de transparência público: lista apenas campanhas ativas.</summary>
    [HttpGet("publicas")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CampanhaPublicaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPublicas(CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(new ListarCampanhasPublicasQuery(), cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CampanhaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(new ObterCampanhaPorIdQuery(id), cancellationToken);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType(typeof(CampanhaResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarCampanhaCommand command, CancellationToken cancellationToken)
    {
        var resultado = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType(typeof(CampanhaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var command = new AtualizarCampanhaCommand(
            id,
            request.Titulo,
            request.Descricao,
            request.DataInicio,
            request.DataFim,
            request.MetaFinanceira,
            request.Imagem,
            request.Status);

        var resultado = await _mediator.Send(command, cancellationToken);
        return Ok(resultado);
    }
}

public record AtualizarCampanhaRequest(
    string Titulo,
    string Descricao,
    DateTime DataInicio,
    DateTime DataFim,
    decimal MetaFinanceira,
    string? Imagem,
    string Status);
