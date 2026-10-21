using Microsoft.AspNetCore.Mvc;
using OpsFlow.Api.Authorization;
using OpsFlow.Application.Features.Reports;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("overview")]
    [HasPermission(PermissionCodes.ReportView)]
    [ProducesResponseType(typeof(ReportOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ReportOverviewDto>> Overview([FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _reports.OverviewAsync(query, cancellationToken));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(MySummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MySummaryDto>> Me([FromQuery] DateOnly? today, CancellationToken cancellationToken)
    {
        return Ok(await _reports.MySummaryAsync(today, cancellationToken));
    }
}
