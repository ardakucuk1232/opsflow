using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Common.Auditing;

public sealed class AuditWriter
{
    private readonly IOpsFlowDbContext _db;
    private readonly IRequestContext _requestContext;
    private readonly TimeProvider _timeProvider;

    public AuditWriter(IOpsFlowDbContext db, IRequestContext requestContext, TimeProvider timeProvider)
    {
        _db = db;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public void Add(
        Guid companyId,
        Guid? actorId,
        string action,
        string entityName,
        string entityId,
        string? label,
        IReadOnlyDictionary<string, object?> oldValues,
        IReadOnlyDictionary<string, object?> newValues)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = companyId,
            UserId = actorId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            EntityLabel = label,
            OldValues = AuditJson.Serialize(oldValues),
            NewValues = AuditJson.Serialize(newValues),
            IpAddress = _requestContext.IpAddress,
            UserAgent = _requestContext.UserAgent is { Length: > 500 } agent ? agent[..500] : _requestContext.UserAgent,
            CreatedAt = _timeProvider.GetUtcNow()
        });
    }
}
