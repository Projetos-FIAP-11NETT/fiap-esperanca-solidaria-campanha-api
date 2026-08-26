using Microsoft.AspNetCore.Mvc;

namespace FiapEsperancaSolidaria.Campanha.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DonationController : ControllerBase
{
    [HttpGet]
    public Task<string> Get() 
    {
        return Task.FromResult("Hello, world.");
    }
}