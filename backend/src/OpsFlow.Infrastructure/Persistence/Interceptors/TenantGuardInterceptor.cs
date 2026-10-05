using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Tenancy;

namespace OpsFlow.Infrastructure.Persistence.Interceptors;

public sealed class TenantGuardInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantGuardInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            Enforce(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Enforce(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void Enforce(DbContext context)
    {
        var tenantId = _tenantContext.CompanyId;

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    EnforceOnAdded(entry, tenantId);
                    break;

                case EntityState.Modified:
                
                case EntityState.Deleted:
                    EnforceOnExisting(entry, tenantId);
                    break;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<Company>().ToList())
        {
            var isChange = entry.State is EntityState.Modified or EntityState.Deleted;

            if (isChange && tenantId is Guid currentTenantId && entry.Entity.Id != currentTenantId)
            {
                throw new TenantIsolationException("A company that is not the current tenant is being modified or deleted.");
            }
        }
    }

    private static void EnforceOnAdded(EntityEntry<ITenantEntity> entry, Guid? tenantId)
    {
        var entity = entry.Entity;
        var entityName = entry.Metadata.ClrType.Name;

        if (tenantId is not Guid currentTenantId)
        {
            if (entity.CompanyId == Guid.Empty)
            {
                throw new TenantIsolationException($"{entityName} is being created without a CompanyId and there is no tenant in the current context.");
            }

            return;
        }

        if (entity.CompanyId == Guid.Empty)
        {
            entity.CompanyId = currentTenantId;
            return;
        }

        if (entity.CompanyId != currentTenantId)
        {
            throw new TenantIsolationException($"{entityName} is being created for a different tenant than the current one.");
        }
    }

    private static void EnforceOnExisting(EntityEntry<ITenantEntity> entry, Guid? tenantId)
    {
        var entityName = entry.Metadata.ClrType.Name;

        var companyIdChanged = entry.State == EntityState.Modified && entry.Property(nameof(ITenantEntity.CompanyId)).IsModified;

        if (companyIdChanged)
        {
            throw new TenantIsolationException($"The CompanyId of an existing {entityName} cannot be changed.");
        }

        if (tenantId is Guid currentTenantId && entry.Entity.CompanyId != currentTenantId)
        {
            throw new TenantIsolationException($"{entityName} belongs to a different tenant than the current one.");
        }
    }
}