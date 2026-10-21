using OpsFlow.Application.Common.Pagination;

namespace OpsFlow.Application.Features.AuditLogs;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken);
}
