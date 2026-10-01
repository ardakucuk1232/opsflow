using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;

public class Role : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}