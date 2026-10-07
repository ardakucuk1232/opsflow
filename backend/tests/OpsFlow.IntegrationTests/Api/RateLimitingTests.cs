using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Auth;
using OpsFlow.Domain.Exceptions;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

public class RateLimitingTests : IClassFixture<RateLimitedApiFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(RateLimitedApiFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task AuthEndpoints_Return429_OnceTheLimitIsExceeded()
    {
        var wrongCredentials = new LoginRequest("nobody@test.local", "Wrong-password-1");

        for (var attempt = 1; attempt <= RateLimitedApiFactory.Limit; attempt++)
        {
            var allowed = await _client.PostAsJsonAsync("/api/auth/login", wrongCredentials);

            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var rejected = await _client.PostAsJsonAsync("/api/auth/login", wrongCredentials);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
        Assert.Equal(ErrorCodes.TooManyRequests, (await rejected.ReadProblemAsync()).Code());

        var refresh = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest("any-token"));

        Assert.Equal(HttpStatusCode.TooManyRequests, refresh.StatusCode);

        var status = await _client.GetAsync("/api/status");

        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
    }
}