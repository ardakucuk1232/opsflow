using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpsFlow.Application.Common.Auditing;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Domain.Common;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Persistence.Auditing;

namespace OpsFlow.Infrastructure.Persistence.Interceptors;

public sealed class AuditTrailInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly IRequestContext _requestContext;
    private readonly TimeProvider _timeProvider;

    public AuditTrailInterceptor(ITenantContext tenantContext, IRequestContext requestContext, TimeProvider timeProvider)
    {
        _tenantContext = tenantContext;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var newRoles = entries
            .Where(e => e is { State: EntityState.Added, Entity: Role })
            .Select(e => ((Role)e.Entity).Id)
            .ToHashSet();

        var now = _timeProvider.GetUtcNow();

        foreach (var entry in entries)
        {
            if (!AuditProfiles.TryGet(entry.Metadata.ClrType, out var profile))
            {
                continue;
            }

            if (entry.Entity is RolePermission rolePermission && newRoles.Contains(rolePermission.RoleId))
            {
                continue;
            }

            var log = CreateLog(context, entry, profile, now);

            if (log is null)
            {
                continue;
            }

            if (entry is { State: EntityState.Added, Entity: Role role })
            {
                log.NewValues = WithPermissions(log.NewValues, entries, role.Id);
            }

            context.Add(log);
        }
    }

    private AuditLog? CreateLog(DbContext context, EntityEntry entry, AuditProfile profile, DateTimeOffset now)
    {
        var softDeleted = entry is { State: EntityState.Modified, Entity: ISoftDeletable }
            && entry.Property(nameof(ISoftDeletable.IsDeleted)) is { IsModified: true, CurrentValue: true };

        var action = entry.State switch
        {
            EntityState.Added => AuditActions.Created,
            EntityState.Deleted => AuditActions.Deleted,
            _ when softDeleted => AuditActions.Deleted,
            _ => AuditActions.Updated
        };

        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            if (AuditProfiles.AlwaysIgnored.Contains(name) || profile.Ignored.Contains(name))
            {
                continue;
            }

            var masked = profile.Masked.Contains(name);

            switch (action)
            {
                case AuditActions.Created:
                    newValues[name] = masked ? AuditJson.MaskedValue : property.CurrentValue;
                    break;

                case AuditActions.Deleted:
                    oldValues[name] = masked ? AuditJson.MaskedValue : property.OriginalValue;
                    break;

                default:
                    if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                    {
                        oldValues[name] = masked ? AuditJson.MaskedValue : property.OriginalValue;
                        newValues[name] = masked ? AuditJson.MaskedValue : property.CurrentValue;
                    }

                    break;
            }
        }

        if (action == AuditActions.Updated && newValues.Count == 0)
        {
            return null;
        }

        var label = profile.Label(entry.Entity, context);

        return new AuditLog
        {
            CompanyId = entry.Entity is Company company ? company.Id : (Guid)entry.Property(nameof(ITenantEntity.CompanyId)).CurrentValue!,
            UserId = _tenantContext.UserId,
            Action = action,
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = string.Join(",", entry.Metadata.FindPrimaryKey()!.Properties
                .Where(p => p.Name != nameof(ITenantEntity.CompanyId))
                .Select(p => entry.Property(p.Name).CurrentValue)),
            EntityLabel = label is null ? null : Truncate(label, AuditLog.EntityLabelMaxLength),
            OldValues = AuditJson.Serialize(oldValues),
            NewValues = AuditJson.Serialize(newValues),
            IpAddress = _requestContext.IpAddress,
            UserAgent = _requestContext.UserAgent is { } agent ? Truncate(agent, 500) : null,
            CreatedAt = now
        };
    }

    private static string? WithPermissions(string? newValues, IEnumerable<EntityEntry> entries, Guid roleId)
    {
        var permissions = entries
            .Where(e => e is { State: EntityState.Added, Entity: RolePermission rp } && rp.RoleId == roleId)
            .Select(e => AuditProfiles.PermissionCode(((RolePermission)e.Entity).PermissionId))
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();

        var values = newValues is null
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(newValues, AuditJson.Options)!;

        values["Permissions"] = permissions;

        return JsonSerializer.Serialize(values, AuditJson.Options);
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
}
