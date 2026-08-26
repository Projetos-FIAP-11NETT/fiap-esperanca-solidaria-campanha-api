using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FiapEsperancaSolidaria.Campanha.Api.Controllers;

[ApiController]
[Route("api/v1/donations")]
public class DonationController : ControllerBase
{
    private readonly IMediator _mediator;

    public DonationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateDonationCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
