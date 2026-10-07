using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OpsFlow.Application.Common.Persistence;
using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Persistence;
using OpsFlow.Infrastructure.Tenancy;

namespace OpsFlow.ArchitectureTests;

public class PersistenceConventionTests
{
    private static readonly IModel Model = BuildModel();

    [Fact]
    public void EveryTenantEntity_HasTheTenantQueryFilter()
    {
        var violations = Model.GetEntityTypes()
            .Where(entityType => typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            .Where(entityType => !HasFilter(entityType, QueryFilterNames.Tenant))
            .Select(entityType => entityType.ClrType.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"These tenant entities have no tenant query filter: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EverySoftDeletableEntity_HasTheSoftDeleteQueryFilter()
    {
        var violations = Model.GetEntityTypes()
            .Where(entityType => typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            .Where(entityType => !HasFilter(entityType, QueryFilterNames.SoftDelete))
            .Select(entityType => entityType.ClrType.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"These soft deletable entities have no soft delete query filter: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Company_HasTheTenantQueryFilter()
    {
        var company = Model.FindEntityType(typeof(Company));

        Assert.NotNull(company);
        Assert.True(HasFilter(company, QueryFilterNames.Tenant));
    }

    private static bool HasFilter(IReadOnlyEntityType entityType, string filterName) =>
        entityType.GetDeclaredQueryFilters().Any(filter => filter.Key == filterName);

    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<OpsFlowDbContext>()
            .UseNpgsql("Host=unused;Database=unused")
            .Options;

        using var db = new OpsFlowDbContext(options, new TenantContext());

        return db.Model;
    }
}
