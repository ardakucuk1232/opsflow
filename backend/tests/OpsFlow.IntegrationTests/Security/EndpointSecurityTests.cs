using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.IntegrationTests.Fixtures;

namespace OpsFlow.IntegrationTests.Security;

[Collection(ApiCollection.Name)]
public partial class EndpointSecurityTests
{
    private static readonly string[] ExpectedAnonymousEndpoints =
    [
        "GET api/Status",
        "POST api/auth/forgot-password",
        "POST api/auth/invitations/accept",
        "POST api/auth/invitations/preview",
        "POST api/auth/login",
        "POST api/auth/logout",
        "POST api/auth/refresh",
        "POST api/auth/register",
        "POST api/auth/reset-password",
        "POST api/auth/verify-email"
    ];

    private readonly OpsFlowApiFactory _factory;

    public EndpointSecurityTests(OpsFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void OnlyTheExpectedEndpoints_AllowAnonymousAccess()
    {
        var anonymous = Endpoints()
            .Where(e => e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .SelectMany(e => e.Methods.Select(method => $"{method} {e.Route}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedAnonymousEndpoints.Order(StringComparer.Ordinal), anonymous);
    }

    [Fact]
    public async Task EveryOtherEndpoint_RejectsRequestsWithoutAToken()
    {
        var client = _factory.CreateApiClient();
        var failures = new List<string>();

        foreach (var (endpoint, route, methods) in Endpoints())
        {
            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            {
                continue;
            }

            foreach (var method in methods)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), "/" + FillParameters(route));

                if (method is "POST" or "PUT")
                {
                    request.Content = JsonContent.Create(new { });
                }

                var response = await client.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    failures.Add($"{method} {route} -> {(int)response.StatusCode}");
                }
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public async Task AForgedToken_IsRejected()
    {
        var client = _factory.CreateApiClient();
        const string forged =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiIwMTk5MDAwMC0wMDAwLTcwMDAtODAwMC0wMDAwMDAwMDAwMDEiLCJjb21wYW55X2lkIjoiMDE5OTAwMDAtMDAwMC03MDAwLTgwMDAtMDAwMDAwMDAwMDAyIn0." +
            "c2lnbmVkLXdpdGgtYS1ndWVzc2VkLWtleQ";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/projects");
        request.Headers.Authorization = new("Bearer", forged);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private IEnumerable<(RouteEndpoint Endpoint, string Route, IReadOnlyList<string> Methods)> Endpoints() =>
        _factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => (Endpoint: e, Route: e.RoutePattern.RawText!.TrimStart('/'), Methods: e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []))
            .Where(e => e.Methods.Count > 0);

    private static string FillParameters(string route) =>
        RouteParameter().Replace(route, match => match.Groups["constraint"].Value.Contains("int") ? "1" : Guid.NewGuid().ToString());

    [GeneratedRegex(@"\{(?<name>[^}:?]+)(:(?<constraint>[^}?]+))?\??\}")]
    private static partial Regex RouteParameter();
}
