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
    public async Task Login_Returns429_OnceTheAuthLimitIsExceeded()
    {
        var wrongCredentials = new LoginRequest("nobody@test.local", "Wrong-password-1");

        for (var attempt = 1; attempt <= RateLimitedApiFactory.AuthLimit; attempt++)
        {
            var allowed = await _client.PostAsJsonAsync("/api/auth/login", wrongCredentials);

            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var rejected = await _client.PostAsJsonAsync("/api/auth/login", wrongCredentials);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
        Assert.Equal(ErrorCodes.TooManyRequests, (await rejected.ReadProblemAsync()).Code());

        var register = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Company", "Test", "User", "limited@test.local", "Test-password-1"));

        Assert.Equal(HttpStatusCode.TooManyRequests, register.StatusCode);

        var status = await _client.GetAsync("/api/status");

        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
    }

    [Fact]
    public async Task Refresh_HasItsOwnLimit_SeparateFromLogin()
    {
        var unknownToken = new RefreshTokenRequest("unknown-token");

        for (var attempt = 1; attempt <= RateLimitedApiFactory.SessionLimit; attempt++)
        {
            var allowed = await _client.PostAsJsonAsync("/api/auth/refresh", unknownToken);

            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var rejected = await _client.PostAsJsonAsync("/api/auth/refresh", unknownToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        var logout = await _client.PostAsJsonAsync("/api/auth/logout", unknownToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, logout.StatusCode);
    }
}
