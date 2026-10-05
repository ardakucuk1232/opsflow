using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpsFlow.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public StatusController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    public ActionResult<StatusResponse> Get()
    {
        var response = new StatusResponse(
            Service: "OpsFlow API",
            Version: "0.1.0",
            Environment: _environment.EnvironmentName,
            ServerTimeUtc: DateTimeOffset.UtcNow);

        return Ok(response);
    }
}

public sealed record StatusResponse(
    string Service,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc);