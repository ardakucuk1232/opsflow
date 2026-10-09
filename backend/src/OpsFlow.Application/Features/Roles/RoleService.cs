using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Security;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Roles;

public sealed class RoleService : IRoleService
{
    private static readonly Expression<Func<Role, RoleDto>> ToDto = r => new RoleDto(
        r.Id,
        r.Name,
        r.Description,
        r.IsSystemRole,
        r.RolePermissions
            .Select(rp => rp.Permission.Code)
            .OrderBy(code => code)
            .ToList(),
        r.UserRoles.Count(ur => !ur.User.IsDeleted));

    private readonly IOpsFlowDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly PrivilegeGuard _privilegeGuard;
    private readonly IValidator<SaveRoleRequest> _validator;

    public RoleService(
        IOpsFlowDbContext db,
        ITenantContext tenantContext,
        PrivilegeGuard privilegeGuard,
        IValidator<SaveRoleRequest> validator)
    {
        _db = db;
        _tenantContext = tenantContext;
        _privilegeGuard = privilegeGuard;
        _validator = validator;
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Roles
            .AsNoTracking()
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.Name)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionCatalogAsync(CancellationToken cancellationToken)
    {
        return await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Group)
            .ThenBy(p => p.Code)
            .Select(p => new PermissionDto(p.Code, p.Group, p.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDto> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        if (_tenantContext.CompanyId is not Guid companyId)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        var permissions = await LoadPermissionsAsync(request.Permissions, cancellationToken);

        await _privilegeGuard.EnsureHoldsAllAsync(permissions.Select(p => p.Code), cancellationToken);

        var role = new Role
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            IsSystemRole = false
        };

        foreach (var permission in permissions)
        {
            role.RolePermissions.Add(new RolePermission
            {
                CompanyId = companyId,
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }

        _db.Roles.Add(role);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(role.Id, cancellationToken);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var role = await LoadEditableRoleAsync(id, cancellationToken);
        var permissions = await LoadPermissionsAsync(request.Permissions, cancellationToken);

        await _privilegeGuard.EnsureHoldsAllAsync(
            role.RolePermissions.Select(rp => rp.Permission.Code).Concat(permissions.Select(p => p.Code)),
            cancellationToken);

        role.Name = request.Name.Trim();
        role.Description = NormalizeDescription(request.Description);

        var newPermissionIds = permissions.Select(p => p.Id).ToHashSet();

        foreach (var rolePermission in role.RolePermissions.Where(rp => !newPermissionIds.Contains(rp.PermissionId)).ToList())
        {
            role.RolePermissions.Remove(rolePermission);
        }

        foreach (var permission in permissions.Where(p => role.RolePermissions.All(rp => rp.PermissionId != p.Id)))
        {
            role.RolePermissions.Add(new RolePermission
            {
                CompanyId = role.CompanyId,
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(role.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await LoadEditableRoleAsync(id, cancellationToken);

        await _privilegeGuard.EnsureHoldsAllAsync(
            role.RolePermissions.Select(rp => rp.Permission.Code),
            cancellationToken);

        var inUse = await _db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id, cancellationToken);

        if (inUse)
        {
            throw new ConflictException(ErrorCodes.Roles.InUse, "The role is assigned to users and cannot be deleted.");
        }

        _db.Roles.Remove(role);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<RoleDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Roles
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(ToDto)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("The role was not found.");
    }

    private async Task<Role> LoadEditableRoleAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("The role was not found.");

        if (role.IsSystemRole)
        {
            throw new BusinessRuleException(
                ErrorCodes.Roles.SystemRoleLocked,
                "System roles cannot be changed or deleted.");
        }

        return role;
    }

    private async Task<List<Permission>> LoadPermissionsAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken)
    {
        var distinctCodes = codes.Distinct(StringComparer.Ordinal).ToList();

        var permissions = await _db.Permissions
            .Where(p => distinctCodes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        if (permissions.Count != distinctCodes.Count)
        {
            throw new BusinessRuleException(
                ErrorCodes.Roles.InvalidPermissions,
                "One or more permissions do not exist.");
        }

        return permissions;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
