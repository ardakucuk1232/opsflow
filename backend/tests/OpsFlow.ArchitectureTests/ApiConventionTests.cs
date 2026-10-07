using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace OpsFlow.ArchitectureTests;

public class ApiConventionTests
{
    private const string ControllersNamespace = "OpsFlow.Api.Controllers";

    private static readonly Assembly Api = typeof(Program).Assembly;

    private static IEnumerable<Type> Controllers => Api.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(type));

    [Fact]
    public void Controllers_LiveInTheControllersNamespace()
    {
        var violations = Controllers
            .Where(type => type.Namespace != ControllersNamespace)
            .Select(type => type.FullName)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"These controllers are outside {ControllersNamespace}: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Controllers_AreSealedAndNamedWithTheControllerSuffix()
    {
        var violations = Controllers
            .Where(type => !type.IsSealed || !type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"These controllers must be sealed and end with 'Controller': {string.Join(", ", violations)}");
    }

    [Fact]
    public void Controllers_AreMarkedAsApiControllers()
    {
        var violations = Controllers
            .Where(type => type.GetCustomAttribute<ApiControllerAttribute>() is null)
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"These controllers are missing [ApiController]: {string.Join(", ", violations)}");
    }
}
