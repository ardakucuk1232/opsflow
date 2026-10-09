using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Auth;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Exceptions;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class PermissionAuthorizationTests
{
    private readonly HttpClient _client;
    private readonly TeamScenario _team;

    public PermissionAuthorizationTests(OpsFlowApiFactory factory)
    {
        _client = factory.CreateApiClient();
        _team = new TeamScenario(factory, _client);
    }

    [Fact]
    public async Task Me_ForTheCompanyAdmin_ListsEveryPermission()
    {
        var admin = await _team.RegisterAdminAsync();

        var me = await _client.GetCurrentUserAsync(admin.AccessToken);

        Assert.Equal(
            SystemRolePermissions.Map[SystemRoles.Admin].Order(StringComparer.Ordinal),
            me.Permissions);
    }

    [Fact]
    public async Task Me_ForAnEmployee_ListsOnlyTheEmployeePermissions()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);

        var me = await _client.GetCurrentUserAsync(employee.AccessToken);

        Assert.Equal(
            SystemRolePermissions.Map[SystemRoles.Employee].Order(StringComparer.Ordinal),
            me.Permissions);
        Assert.Equal([SystemRoles.Employee], me.Roles);
    }

    [Fact]
    public async Task Employee_CanListUsers_ButCannotInviteOrManageRoles()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);

        var list = await _team.SendAsync(employee, HttpMethod.Get, "/api/users");
        var invite = await _team.SendAsync(employee, HttpMethod.Post, "/api/users/invitations", TeamScenario.NewInvitation(employeeRoleId));
        var catalog = await _team.SendAsync(employee, HttpMethod.Get, "/api/roles/permissions");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, invite.StatusCode);
        Assert.Equal(ErrorCodes.Forbidden, (await invite.ReadProblemAsync()).Code());
        Assert.Equal(HttpStatusCode.Forbidden, catalog.StatusCode);
    }

    [Fact]
    public async Task ARoleChange_TakesEffectWithoutANewAccessToken()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);

        var before = await _team.SendAsync(employee, HttpMethod.Post, "/api/users/invitations", TeamScenario.NewInvitation(employeeRoleId));
        Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);

        var recruiter = await _team.CreateRoleAsync(admin, "Recruiter", PermissionCodes.UserView, PermissionCodes.UserInvite);
        await _team.AssignRolesAsync(admin, employee.Id, employeeRoleId, recruiter.Id);

        var after = await _team.SendAsync(employee, HttpMethod.Post, "/api/users/invitations", TeamScenario.NewInvitation(employeeRoleId));
        Assert.Equal(HttpStatusCode.Created, after.StatusCode);
    }

    [Fact]
    public async Task AUserWhoCanInvite_CannotHandOutRolesWithMorePermissionsThanTheirOwn()
    {
        var admin = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);
        var adminRoleId = await _team.RoleIdAsync(admin, SystemRoles.Admin);

        var recruiter = await _team.CreateRoleAsync(admin, "Recruiter", PermissionCodes.UserView, PermissionCodes.UserInvite);
        await _team.AssignRolesAsync(admin, member.Id, employeeRoleId, recruiter.Id);

        var asEmployee = await _team.SendAsync(member, HttpMethod.Post, "/api/users/invitations", TeamScenario.NewInvitation(employeeRoleId));
        var asAdmin = await _team.SendAsync(member, HttpMethod.Post, "/api/users/invitations", TeamScenario.NewInvitation(adminRoleId));

        Assert.Equal(HttpStatusCode.Created, asEmployee.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, asAdmin.StatusCode);
        Assert.Equal(ErrorCodes.InsufficientPrivileges, (await asAdmin.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task ADeactivatedUser_LosesAccessImmediately_AndRegainsItWhenActivated()
    {
        var admin = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(admin, SystemRoles.Manager);

        var deactivate = await _team.SendAsync(admin, HttpMethod.Post, $"/api/users/{member.Id}/deactivate");
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var withOldToken = await _team.SendAsync(member, HttpMethod.Get, "/api/users");
        var refresh = await _client.RefreshAsync(member.RefreshToken);
        var login = await _client.LoginAsync(member.Email, member.Password);

        Assert.Equal(HttpStatusCode.Forbidden, withOldToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Equal(ErrorCodes.Auth.AccountDisabled, (await login.ReadProblemAsync()).Code());

        var activate = await _team.SendAsync(admin, HttpMethod.Post, $"/api/users/{member.Id}/activate");
        Assert.Equal(HttpStatusCode.NoContent, activate.StatusCode);

        var loginAgain = await _client.LoginAsync(member.Email, member.Password);
        Assert.Equal(HttpStatusCode.OK, loginAgain.StatusCode);
    }
}
