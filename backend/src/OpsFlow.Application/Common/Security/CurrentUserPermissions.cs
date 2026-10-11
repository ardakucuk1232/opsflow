using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Caching;
using OpsFlow.Application.Common.Interfaces;

namespace OpsFlow.Application.Common.Security;

public sealed class CurrentUserPermissions : ICurrentUserPermissions
{
    private static readonly IReadOnlySet<string> None = new HashSet<string>();

    private readonly IOpsFlowDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IAppCache _cache;
    private IReadOnlySet<string>? _permissions;

    public CurrentUserPermissions(IOpsFlowDbContext db, ITenantContext tenantContext, IAppCache cache)
    {
        _db = db;
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task<IReadOnlySet<string>> GetAsync(CancellationToken cancellationToken)
    {
        if (_permissions is not null)
        {
            return _permissions;
        }

        if (_tenantContext.UserId is not Guid userId)
        {
            return None;
        }

        var codes = await _cache.GetOrCreateAsync(
            CacheKeys.Permissions(userId),
            token => LoadAsync(userId, token),
            CacheKeys.PermissionsLifetime,
            cancellationToken);

        _permissions = codes.ToHashSet(StringComparer.Ordinal);

        return _permissions;
    }

    public async Task<bool> HasAsync(string permission, CancellationToken cancellationToken)
    {
        var permissions = await GetAsync(cancellationToken);

        return permissions.Contains(permission);
    }

    private async Task<List<string>> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.User.IsActive && ur.User.Company.IsActive)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
