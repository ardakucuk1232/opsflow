using System.Net;
using OpsFlow.IntegrationTests.Fixtures;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class CorsTests
{
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";

    private readonly HttpClient _client;

    public CorsTests(OpsFlowApiFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_IsApproved()
    {
        using var request = CreatePreflight(OpsFlowApiFactory.AllowedOrigin);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(AllowOriginHeader, out var values));
        Assert.Equal(OpsFlowApiFactory.AllowedOrigin, Assert.Single(values));
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_GetsNoCorsHeaders()
    {
        using var request = CreatePreflight("http://unknown-site.example");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains(AllowOriginHeader));
    }

    [Fact]
    public async Task ErrorResponse_ToAllowedOrigin_StillCarriesCorsHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Origin", OpsFlowApiFactory.AllowedOrigin);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.Contains(AllowOriginHeader));
    }

    private static HttpRequestMessage CreatePreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");

        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        return request;
    }
}