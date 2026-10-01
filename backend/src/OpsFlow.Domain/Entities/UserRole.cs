using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;

public class UserRole : ITenantEntity
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    public Company Company { get; set; } = null!;
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}