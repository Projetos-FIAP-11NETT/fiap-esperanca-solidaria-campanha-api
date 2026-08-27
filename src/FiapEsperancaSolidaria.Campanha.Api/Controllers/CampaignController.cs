using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CreateCampaign;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaign;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.GetCampaignById;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.ListPublicCampaigns;
using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapEsperancaSolidaria.Campanha.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CampaignController : ControllerBase
{
    private readonly IMediator _mediator;

    public CampaignController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Painel de transparência público: lista campanhas ativas, opcionalmente filtradas por título.</summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PublicCampaignResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPublic([FromQuery] string? title, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPublicCampaignsQuery(title), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCampaignByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCampaignCommand(
            id,
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.FinancialGoal,
            request.Image,
            request.Status);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

public record UpdateCampaignRequest(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal FinancialGoal,
    string? Image,
    CampaignStatus Status);
