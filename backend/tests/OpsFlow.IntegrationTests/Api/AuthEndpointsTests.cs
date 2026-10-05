using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Auth;
using OpsFlow.IntegrationTests.Fixtures;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests
{
    private const string Password = "Test-password-1";

    private readonly HttpClient _client;

    public AuthEndpointsTests(OpsFlowApiFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Register_ThenMe_ReturnsTheRegisteredUserAsAdmin()
    {
        var registration = NewRegistration();

        var auth = await RegisterAsync(registration);
        var response = await GetMeAsync(auth.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<AuthUserDto>();

        Assert.NotNull(me);
        Assert.Equal(registration.Email, me.Email);
        Assert.Equal(auth.User.CompanyId, me.CompanyId);
        Assert.Contains("Admin", me.Roles);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401AsProblemDetails()
    {
        var response = await GetMeAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var registration = NewRegistration();
        await RegisterAsync(registration);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(registration.Email, "Wrong-password-1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAnEmailThatIsAlreadyUsed_Returns409()
    {
        var registration = NewRegistration();
        await RegisterAsync(registration);

        var response = await _client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesTheToken_AndReuseRevokesTheSession()
    {
        var auth = await RegisterAsync(NewRegistration());

        var firstRefresh = await RefreshAsync(auth.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        var rotated = await firstRefresh.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(auth.RefreshToken, rotated.RefreshToken);

        var reuse = await RefreshAsync(auth.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var afterAlarm = await RefreshAsync(rotated.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, afterAlarm.StatusCode);
    }

    [Fact]
    public async Task Me_ForTwoCompanies_ReturnsEachUsersOwnCompany()
    {
        var authA = await RegisterAsync(NewRegistration());
        var authB = await RegisterAsync(NewRegistration());

        var meA = await (await GetMeAsync(authA.AccessToken)).Content.ReadFromJsonAsync<AuthUserDto>();
        var meB = await (await GetMeAsync(authB.AccessToken)).Content.ReadFromJsonAsync<AuthUserDto>();

        Assert.NotNull(meA);
        Assert.NotNull(meB);
        Assert.Equal(authA.User.CompanyId, meA.CompanyId);
        Assert.Equal(authB.User.CompanyId, meB.CompanyId);
        Assert.NotEqual(meA.CompanyId, meB.CompanyId);
    }

    private static RegisterRequest NewRegistration()
    {
        var unique = Guid.NewGuid().ToString("N");

        return new RegisterRequest(
            CompanyName: $"Company {unique}",
            FirstName: "Test",
            LastName: "User",
            Email: $"user-{unique}@test.local",
            Password: Password);
    }

    private async Task<AuthResponse> RegisterAsync(RegisterRequest registration)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        return auth;
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(refreshToken));

    private async Task<HttpResponseMessage> GetMeAsync(string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await _client.SendAsync(request);
    }
}