using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Features.Auth;
using OpsFlow.Application.Features.Users;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Exceptions;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class UserEndpointsTests
{
    private static readonly TimeSpan PastCooldown = TimeSpan.FromMinutes(5);

    private readonly OpsFlowApiFactory _factory;
    private readonly HttpClient _client;
    private readonly TeamScenario _team;

    public UserEndpointsTests(OpsFlowApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
        _team = new TeamScenario(factory, _client);
    }

    [Fact]
    public async Task List_ReturnsOnlyUsersOfTheCurrentCompany()
    {
        var adminA = await _team.RegisterAdminAsync();
        var memberA = await _team.AddMemberAsync(adminA, SystemRoles.Employee);
        var adminB = await _team.RegisterAdminAsync();
        var memberB = await _team.AddMemberAsync(adminB, SystemRoles.Employee);

        var page = await _team.GetAsync<PagedResult<UserSummaryDto>>(adminA, "/api/users");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(
            new[] { adminA.Id, memberA.Id }.Order(),
            page.Items.Select(u => u.Id).Order());
        Assert.DoesNotContain(page.Items, u => u.Id == adminB.Id || u.Id == memberB.Id);
    }

    [Fact]
    public async Task Get_AUserOfAnotherCompany_Returns404()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();

        var response = await _team.SendAsync(adminA, HttpMethod.Get, $"/api/users/{adminB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByStatusSearchAndRole_AndPaginates()
    {
        var admin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);
        var manager = await _team.AddMemberAsync(admin, SystemRoles.Manager);
        var invited = await _team.InviteAsync(admin, employeeRoleId);
        var inactive = await _team.AddMemberAsync(admin, SystemRoles.Employee);

        await _team.SendAsync(admin, HttpMethod.Post, $"/api/users/{inactive.Id}/deactivate");

        var active = await _team.GetAsync<PagedResult<UserSummaryDto>>(admin, "/api/users?status=Active");
        var pending = await _team.GetAsync<PagedResult<UserSummaryDto>>(admin, "/api/users?status=Invited");
        var deactivated = await _team.GetAsync<PagedResult<UserSummaryDto>>(admin, "/api/users?status=Inactive");
        var employees = await _team.GetAsync<PagedResult<UserSummaryDto>>(admin, $"/api/users?roleId={employeeRoleId}");
        var search = await _team.GetAsync<PagedResult<UserSummaryDto>>(admin, $"/api/users?search={Uri.EscapeDataString(manager.Email.ToUpperInvariant())}");
        var firstPage = await _team.GetAsync<PagedResult<UserSummaryDto>>(admin, "/api/users?page=1&pageSize=1");

        Assert.Equal(new[] { admin.Id, manager.Id }.Order(), active.Items.Select(u => u.Id).Order());
        Assert.True(Assert.Single(pending.Items).InvitationPending);
        Assert.Equal(invited.Id, pending.Items[0].Id);
        Assert.Equal(inactive.Id, Assert.Single(deactivated.Items).Id);
        Assert.Equal(new[] { invited.Id, inactive.Id }.Order(), employees.Items.Select(u => u.Id).Order());
        Assert.Equal(manager.Id, Assert.Single(search.Items).Id);
        Assert.Equal(4, firstPage.TotalCount);
        Assert.Single(firstPage.Items);
        Assert.True(firstPage.HasNextPage);
    }

    [Fact]
    public async Task Invite_ByAnAdminWhoseEmailIsNotVerified_Returns422()
    {
        var admin = await _team.RegisterAdminAsync(verifyEmail: false);
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);

        var response = await _team.SendAsync(admin, HttpMethod.Post, "/api/users/invitations", TeamScenario.NewInvitation(employeeRoleId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ErrorCodes.Users.EmailNotVerified, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task Invite_EmailsAnInvitationAndTheUserCannotSignInBeforeAccepting()
    {
        var admin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);

        var invited = await _team.InviteAsync(admin, employeeRoleId);

        Assert.True(invited.InvitationPending);
        Assert.False(invited.IsEmailVerified);
        Assert.Equal(SystemRoles.Employee, Assert.Single(invited.Roles).Name);

        var email = Assert.Single(_factory.Emails.SentTo(invited.Email));
        Assert.Contains($"{OpsFlowApiFactory.FrontendBaseUrl}/accept-invitation#token=", email.TextBody);

        var login = await _client.LoginAsync(invited.Email, "Any-password-1");

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(ErrorCodes.Auth.InvalidCredentials, (await login.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task Invite_WithAnEmailThatIsAlreadyUsed_Returns409()
    {
        var admin = await _team.RegisterAdminAsync();
        var otherAdmin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);

        var request = TeamScenario.NewInvitation(employeeRoleId) with { Email = otherAdmin.Email.ToUpperInvariant() };
        var response = await _team.SendAsync(admin, HttpMethod.Post, "/api/users/invitations", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ErrorCodes.Auth.EmailAlreadyInUse, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task Invite_WithARoleOfAnotherCompany_Returns422()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();
        var foreignRoleId = await _team.RoleIdAsync(adminB, SystemRoles.Employee);

        var response = await _team.SendAsync(adminA, HttpMethod.Post, "/api/users/invitations", TeamScenario.NewInvitation(foreignRoleId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ErrorCodes.Users.InvalidRoles, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task AcceptingAnInvitation_SetsThePasswordAndSignsTheUserIn()
    {
        var admin = await _team.RegisterAdminAsync();
        var managerRoleId = await _team.RoleIdAsync(admin, SystemRoles.Manager);
        var invited = await _team.InviteAsync(admin, managerRoleId);
        var token = _team.LatestTokenSentTo(invited.Email);

        var preview = await _client.PostAsJsonAsync("/api/auth/invitations/preview", new InvitationTokenRequest(token));
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);

        var details = await preview.Content.ReadFromJsonAsync<InvitationPreviewDto>();
        var adminMe = await _client.GetCurrentUserAsync(admin.AccessToken);
        Assert.NotNull(details);
        Assert.Equal(invited.Email, details.Email);
        Assert.Equal(adminMe.CompanyName, details.CompanyName);

        var accept = await _client.PostAsJsonAsync("/api/auth/invitations/accept", new AcceptInvitationRequest(token, "Fresh-password-1"));
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        var auth = await accept.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(invited.Id, auth.User.Id);
        Assert.Equal(adminMe.CompanyId, auth.User.CompanyId);
        Assert.True(auth.User.IsEmailVerified);
        Assert.Equal([SystemRoles.Manager], auth.User.Roles);

        var again = await _client.PostAsJsonAsync("/api/auth/invitations/accept", new AcceptInvitationRequest(token, "Other-password-2"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Equal(ErrorCodes.Auth.InvalidToken, (await again.ReadProblemAsync()).Code());

        var login = await _client.LoginAsync(invited.Email, "Fresh-password-1");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task AcceptingAnInvitation_WithAWeakPassword_Returns400()
    {
        var admin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);
        var invited = await _team.InviteAsync(admin, employeeRoleId);
        var token = _team.LatestTokenSentTo(invited.Email);

        var accept = await _client.PostAsJsonAsync("/api/auth/invitations/accept", new AcceptInvitationRequest(token, "short"));

        Assert.Equal(HttpStatusCode.BadRequest, accept.StatusCode);
    }

    [Fact]
    public async Task AcceptingAnInvitation_ForADeactivatedUser_IsRejected()
    {
        var admin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);
        var invited = await _team.InviteAsync(admin, employeeRoleId);
        var token = _team.LatestTokenSentTo(invited.Email);

        await _team.SendAsync(admin, HttpMethod.Post, $"/api/users/{invited.Id}/deactivate");

        var accept = await _client.PostAsJsonAsync("/api/auth/invitations/accept", new AcceptInvitationRequest(token, "Fresh-password-1"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, accept.StatusCode);
        Assert.Equal(ErrorCodes.Auth.InvalidToken, (await accept.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task ResendInvitation_AfterTheCooldown_SendsANewLinkAndInvalidatesTheOldOne()
    {
        var admin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);
        var invited = await _team.InviteAsync(admin, employeeRoleId);
        var firstToken = _team.LatestTokenSentTo(invited.Email);

        await _factory.BackdateUserTokensAsync(invited.Id, PastCooldown);

        var resend = await _team.SendAsync(admin, HttpMethod.Post, $"/api/users/{invited.Id}/resend-invitation");
        Assert.Equal(HttpStatusCode.NoContent, resend.StatusCode);

        var secondToken = _team.LatestTokenSentTo(invited.Email);
        Assert.NotEqual(firstToken, secondToken);

        var stale = await _client.PostAsJsonAsync("/api/auth/invitations/preview", new InvitationTokenRequest(firstToken));
        var fresh = await _client.PostAsJsonAsync("/api/auth/invitations/preview", new InvitationTokenRequest(secondToken));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, stale.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
    }

    [Fact]
    public async Task ResendInvitation_ForAUserWhoAlreadyAccepted_Returns422()
    {
        var admin = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(admin, SystemRoles.Employee);

        var resend = await _team.SendAsync(admin, HttpMethod.Post, $"/api/users/{member.Id}/resend-invitation");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resend.StatusCode);
        Assert.Equal(ErrorCodes.Users.InvitationAlreadyAccepted, (await resend.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task UpdateRoles_ReplacesTheRolesOfTheUser()
    {
        var admin = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var managerRoleId = await _team.RoleIdAsync(admin, SystemRoles.Manager);

        var response = await _team.SendAsync(admin, HttpMethod.Put, $"/api/users/{member.Id}/roles", new UpdateUserRolesRequest([managerRoleId]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<UserSummaryDto>();
        Assert.NotNull(updated);
        Assert.Equal(SystemRoles.Manager, Assert.Single(updated.Roles).Name);

        var me = await _client.GetCurrentUserAsync(member.AccessToken);
        Assert.Equal([SystemRoles.Manager], me.Roles);
    }

    [Fact]
    public async Task UpdateRoles_OfYourself_Returns422()
    {
        var admin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);

        var response = await _team.SendAsync(admin, HttpMethod.Put, $"/api/users/{admin.Id}/roles", new UpdateUserRolesRequest([employeeRoleId]));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ErrorCodes.Users.CannotModifySelf, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task UpdateRoles_ThatWouldLeaveTheCompanyWithoutAnAdmin_Returns422()
    {
        var admin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);
        var superuser = await _team.CreateRoleAsync(admin, "Superuser", [.. SystemRolePermissions.Map[SystemRoles.Admin]]);
        var member = await _team.AddMemberAsync(admin, SystemRoles.Employee);

        await _team.AssignRolesAsync(admin, member.Id, superuser.Id);

        var demote = await _team.SendAsync(member, HttpMethod.Put, $"/api/users/{admin.Id}/roles", new UpdateUserRolesRequest([employeeRoleId]));
        var deactivate = await _team.SendAsync(member, HttpMethod.Post, $"/api/users/{admin.Id}/deactivate");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, demote.StatusCode);
        Assert.Equal(ErrorCodes.Users.LastAdmin, (await demote.ReadProblemAsync()).Code());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, deactivate.StatusCode);
        Assert.Equal(ErrorCodes.Users.LastAdmin, (await deactivate.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task Deactivate_Yourself_Returns422()
    {
        var admin = await _team.RegisterAdminAsync();

        var response = await _team.SendAsync(admin, HttpMethod.Post, $"/api/users/{admin.Id}/deactivate");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ErrorCodes.Users.CannotModifySelf, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task Deactivate_AUserOfAnotherCompany_Returns404()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();
        var memberB = await _team.AddMemberAsync(adminB, SystemRoles.Employee);

        var response = await _team.SendAsync(adminA, HttpMethod.Post, $"/api/users/{memberB.Id}/deactivate");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var login = await _client.LoginAsync(memberB.Email, memberB.Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
