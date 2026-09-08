using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CancelCampaign;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CreateCampaign;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaign;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.GetCampaignById;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.ListCampaigns;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.ListPublicCampaigns;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UploadCampaignImage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapEsperancaSolidaria.Campanha.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CampaignController(IMediator mediator) : ControllerBase
{

    /// <summary>Painel de transparência público: lista campanhas ativas, opcionalmente filtradas por título.</summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PublicCampaignResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPublic([FromQuery] string? title, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListPublicCampaignsQuery(title), cancellationToken);
        return Ok(result);
    }

    /// <summary>Lista todas as campanhas (qualquer status) — uso restrito ao gestor.</summary>
    [HttpGet]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType(typeof(IReadOnlyList<CampaignResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListCampaignsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCampaignByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
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
            request.Image);

        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Sobe a imagem de capa de uma campanha pro S3 (LocalStack em dev) e devolve a URL pública.</summary>
    [HttpPost("images")]
    [Authorize(Roles = "GestorONG")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_000_000)]
    [ProducesResponseType(typeof(UploadCampaignImageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest(new { error = "Arquivo vazio." });

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new UploadCampaignImageCommand(stream, file.FileName, file.ContentType),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "GestorONG")]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelCampaignCommand(id), cancellationToken);
        return Ok(result);
    }
}

public record UpdateCampaignRequest(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal FinancialGoal,
    string? Image);
