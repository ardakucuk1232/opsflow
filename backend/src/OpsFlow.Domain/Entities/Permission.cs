using OpsFlow.Domain.Common;

namespace OpsFlow.Domain.Entities;

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}