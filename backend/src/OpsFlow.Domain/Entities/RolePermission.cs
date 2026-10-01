using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;

public class RolePermission : ITenantEntity
{
    public Guid CompanyId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Company Company { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}