using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Auth;
using OpsFlow.Application.Features.Roles;
using OpsFlow.Application.Features.Users;
using OpsFlow.IntegrationTests.Fixtures;

namespace OpsFlow.IntegrationTests.Support;

public sealed record Member(Guid Id, string Email, string Password, string AccessToken, string RefreshToken);

public sealed class TeamScenario
{
    public const string MemberPassword = "Member-password-1";

    private readonly OpsFlowApiFactory _factory;
    private readonly HttpClient _client;

    public TeamScenario(OpsFlowApiFactory factory, HttpClient client)
    {
        _factory = factory;
        _client = client;
    }

    public async Task<Member> RegisterAdminAsync(bool verifyEmail = true)
    {
        var registration = AuthClientExtensions.NewRegistration();
        var auth = await _client.RegisterAsync(registration);

        if (verifyEmail)
        {
            var token = LatestTokenSentTo(registration.Email);
            var response = await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(token));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        return new Member(auth.User.Id, registration.Email, registration.Password, auth.AccessToken, auth.RefreshToken);
    }

    public async Task<Guid> RoleIdAsync(Member actor, string roleName)
    {
        var roles = await GetAsync<List<RoleDto>>(actor, "/api/roles");

        return roles.Single(r => r.Name == roleName).Id;
    }

    public async Task<UserSummaryDto> InviteAsync(Member inviter, params Guid[] roleIds)
    {
        var request = NewInvitation(roleIds);
        var response = await SendAsync(inviter, HttpMethod.Post, "/api/users/invitations", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserSummaryDto>();
        Assert.NotNull(user);

        return user;
    }

    public async Task<Member> AddMemberAsync(Member admin, params string[] roleNames)
    {
        var roleIds = new List<Guid>();

        foreach (var roleName in roleNames)
        {
            roleIds.Add(await RoleIdAsync(admin, roleName));
        }

        var invited = await InviteAsync(admin, [.. roleIds]);
        var token = LatestTokenSentTo(invited.Email);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/invitations/accept",
            new AcceptInvitationRequest(token, MemberPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        return new Member(auth.User.Id, invited.Email, MemberPassword, auth.AccessToken, auth.RefreshToken);
    }

    public async Task<RoleDto> CreateRoleAsync(Member actor, string name, params string[] permissions)
    {
        var response = await SendAsync(actor, HttpMethod.Post, "/api/roles", new SaveRoleRequest(name, null, permissions));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var role = await response.Content.ReadFromJsonAsync<RoleDto>();
        Assert.NotNull(role);

        return role;
    }

    public async Task AssignRolesAsync(Member actor, Guid userId, params Guid[] roleIds)
    {
        var response = await SendAsync(actor, HttpMethod.Put, $"/api/users/{userId}/roles", new UpdateUserRolesRequest(roleIds));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public string LatestTokenSentTo(string address)
    {
        var emails = _factory.Emails.SentTo(address);

        Assert.NotEmpty(emails);

        return CapturingEmailQueue.ExtractToken(emails[^1]);
    }

    public async Task<T> GetAsync<T>(Member actor, string path)
    {
        var response = await SendAsync(actor, HttpMethod.Get, path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(body);

        return body;
    }

    public async Task<HttpResponseMessage> SendAsync(Member actor, HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor.AccessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    public static InviteUserRequest NewInvitation(params Guid[] roleIds)
    {
        var unique = Guid.NewGuid().ToString("N");

        return new InviteUserRequest($"member-{unique}@test.local", "Member", unique[..8], roleIds);
    }
}
