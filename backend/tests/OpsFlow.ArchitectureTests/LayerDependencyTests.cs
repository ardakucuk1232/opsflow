using System.Reflection;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Domain.Common;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(BaseEntity).Assembly;
    private static readonly Assembly Application = typeof(IOpsFlowDbContext).Assembly;
    private static readonly Assembly Infrastructure = typeof(OpsFlowDbContext).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherLayerAndNoFramework()
    {
        AssertHasNoReferenceTo(
            Domain,
            "OpsFlow.",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Microsoft.Extensions",
            "Npgsql",
            "FluentValidation");
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        AssertHasNoReferenceTo(
            Application,
            "OpsFlow.Infrastructure",
            "OpsFlow.Api",
            "Npgsql",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        AssertHasNoReferenceTo(Infrastructure, "OpsFlow.Api");
    }

    private static void AssertHasNoReferenceTo(Assembly assembly, params string[] forbiddenPrefixes)
    {
        var violations = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"{assembly.GetName().Name} must not reference: {string.Join(", ", violations)}");
    }
}
