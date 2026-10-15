using Idempotency.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Idempotency.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(IHealthService healthService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        await healthService.IsReadyAsync(ct)
            ? Ok(new { status = "ready" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable);
}
