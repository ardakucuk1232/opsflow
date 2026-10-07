using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Auth;

namespace OpsFlow.IntegrationTests.Support;

public static class AuthClientExtensions
{
    public const string DefaultPassword = "Test-password-1";

    public static RegisterRequest NewRegistration()
    {
        var unique = Guid.NewGuid().ToString("N");

        return new RegisterRequest(
            CompanyName: $"Company {unique}",
            FirstName: "Test",
            LastName: "User",
            Email: $"user-{unique}@test.local",
            Password: DefaultPassword);
    }

    public static async Task<AuthResponse> RegisterAsync(this HttpClient client, RegisterRequest registration)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        return auth;
    }

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(refreshToken));

    public static Task<HttpResponseMessage> GetMeAsync(this HttpClient client, string? accessToken) =>
        client.SendWithTokenAsync(HttpMethod.Get, "/api/auth/me", accessToken);

    public static async Task<AuthUserDto> GetCurrentUserAsync(this HttpClient client, string accessToken)
    {
        var response = await client.GetMeAsync(accessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        Assert.NotNull(user);

        return user;
    }

    public static async Task<HttpResponseMessage> SendWithTokenAsync(
        this HttpClient client,
        HttpMethod method,
        string path,
        string? accessToken)
    {
        using var request = new HttpRequestMessage(method, path);

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }
}
