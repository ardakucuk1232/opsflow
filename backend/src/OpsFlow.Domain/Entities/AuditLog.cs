using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;

public class AuditLog : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }
    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    public Company Company { get; set; } = null!;
    public User? User { get; set; }

}