using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;
using FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Queries.GetDonationById;
using FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Queries.ListMyDonations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapEsperancaSolidaria.Campanha.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DonationController(IMediator mediator) : ControllerBase
{
    /// <summary>Recibo do próprio doador: todas as doações que ele fez, mesmo em campanhas já encerradas/canceladas.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Doador")]
    [ProducesResponseType(typeof(IReadOnlyList<DonationReceiptResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListMyDonationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDonationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Doador")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateDonationCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}