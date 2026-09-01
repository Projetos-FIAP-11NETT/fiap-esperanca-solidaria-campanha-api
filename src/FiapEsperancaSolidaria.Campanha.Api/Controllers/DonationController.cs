using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.GetCampaignById;
using FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapEsperancaSolidaria.Campanha.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DonationController(IMediator mediator) : ControllerBase
{
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
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateDonationCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}