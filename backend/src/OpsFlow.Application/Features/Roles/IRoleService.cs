namespace OpsFlow.Application.Features.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionDto>> GetPermissionCatalogAsync(CancellationToken cancellationToken);

    Task<RoleDto> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken);

    Task<RoleDto> UpdateAsync(Guid id, SaveRoleRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
