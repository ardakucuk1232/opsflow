using System.Reflection;
using OpsFlow.Domain.Common;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.ArchitectureTests;

public class DomainConventionTests
{
    private const string EntitiesNamespace = "OpsFlow.Domain.Entities";

    private static readonly Assembly Domain = typeof(BaseEntity).Assembly;

    private static IEnumerable<Type> Entities => Domain.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && type.Namespace == EntitiesNamespace);

    [Fact]
    public void EveryEntityWithACompanyId_ImplementsITenantEntity()
    {
        var violations = Entities
            .Where(type => type.GetProperty(nameof(ITenantEntity.CompanyId)) is not null)
            .Where(type => !typeof(ITenantEntity).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"These entities have a CompanyId but do not implement ITenantEntity: {string.Join(", ", violations)}");
    }

    [Fact]
    public void EveryEntityWithIsDeleted_ImplementsISoftDeletable()
    {
        var violations = Entities
            .Where(type => type.GetProperty(nameof(ISoftDeletable.IsDeleted)) is not null)
            .Where(type => !typeof(ISoftDeletable).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"These entities have IsDeleted but do not implement ISoftDeletable: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainExceptions_AreSealed()
    {
        var violations = Domain.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(DomainException)) && !type.IsSealed)
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"These domain exceptions must be sealed: {string.Join(", ", violations)}");
    }
}
