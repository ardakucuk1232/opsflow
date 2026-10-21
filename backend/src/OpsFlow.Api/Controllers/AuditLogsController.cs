using Microsoft.AspNetCore.Mvc;
using OpsFlow.Api.Authorization;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Features.AuditLogs;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[HasPermission(PermissionCodes.AuditLogView)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogs;

    public AuditLogsController(IAuditLogService auditLogs)
    {
        _auditLogs = auditLogs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _auditLogs.ListAsync(query, cancellationToken));
    }
}
