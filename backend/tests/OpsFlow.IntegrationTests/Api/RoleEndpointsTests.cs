using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Roles;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Exceptions;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class RoleEndpointsTests
{
    private readonly TeamScenario _team;

    public RoleEndpointsTests(OpsFlowApiFactory factory)
    {
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task List_ShowsTheSystemRolesWithTheirMembersCount()
    {
        var admin = await _team.RegisterAdminAsync();

        var roles = await _team.GetAsync<List<RoleDto>>(admin, "/api/roles");

        Assert.Equal(
            [SystemRoles.Admin, SystemRoles.Employee, SystemRoles.Manager],
            roles.Select(r => r.Name));
        Assert.All(roles, role => Assert.True(role.IsSystemRole));
        Assert.Equal(1, roles.Single(r => r.Name == SystemRoles.Admin).UserCount);
        Assert.Equal(0, roles.Single(r => r.Name == SystemRoles.Employee).UserCount);
    }

    [Fact]
    public async Task PermissionCatalog_ListsEveryPermission()
    {
        var admin = await _team.RegisterAdminAsync();

        var catalog = await _team.GetAsync<List<PermissionDto>>(admin, "/api/roles/permissions");

        Assert.Equal(
            SystemRolePermissions.Map[SystemRoles.Admin].Order(StringComparer.Ordinal),
            catalog.Select(p => p.Code).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task ACustomRole_CanBeCreatedUpdatedAndDeleted()
    {
        var admin = await _team.RegisterAdminAsync();

        var created = await _team.CreateRoleAsync(admin, "  Auditor  ", PermissionCodes.AuditLogView, PermissionCodes.ReportView);

        Assert.Equal("Auditor", created.Name);
        Assert.False(created.IsSystemRole);
        Assert.Equal([PermissionCodes.AuditLogView, PermissionCodes.ReportView], created.Permissions);

        var update = await _team.SendAsync(
            admin,
            HttpMethod.Put,
            $"/api/roles/{created.Id}",
            new SaveRoleRequest("Senior auditor", "Reads the audit trail", [PermissionCodes.AuditLogView]));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var updated = await update.Content.ReadFromJsonAsync<RoleDto>();
        Assert.NotNull(updated);
        Assert.Equal("Senior auditor", updated.Name);
        Assert.Equal("Reads the audit trail", updated.Description);
        Assert.Equal([PermissionCodes.AuditLogView], updated.Permissions);

        var delete = await _team.SendAsync(admin, HttpMethod.Delete, $"/api/roles/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var roles = await _team.GetAsync<List<RoleDto>>(admin, "/api/roles");
        Assert.DoesNotContain(roles, r => r.Id == created.Id);
    }

    [Fact]
    public async Task Create_WithANameThatIsTaken_Returns409()
    {
        var admin = await _team.RegisterAdminAsync();

        var response = await _team.SendAsync(admin, HttpMethod.Post, "/api/roles", new SaveRoleRequest(SystemRoles.Manager, null, []));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ErrorCodes.Roles.NameTaken, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task Create_WithAnUnknownPermission_Returns422()
    {
        var admin = await _team.RegisterAdminAsync();

        var response = await _team.SendAsync(admin, HttpMethod.Post, "/api/roles", new SaveRoleRequest("Hacker", null, ["root.everything"]));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ErrorCodes.Roles.InvalidPermissions, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task SystemRoles_CannotBeChangedOrDeleted()
    {
        var admin = await _team.RegisterAdminAsync();
        var employeeRoleId = await _team.RoleIdAsync(admin, SystemRoles.Employee);

        var update = await _team.SendAsync(admin, HttpMethod.Put, $"/api/roles/{employeeRoleId}", new SaveRoleRequest(SystemRoles.Employee, null, []));
        var delete = await _team.SendAsync(admin, HttpMethod.Delete, $"/api/roles/{employeeRoleId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, update.StatusCode);
        Assert.Equal(ErrorCodes.Roles.SystemRoleLocked, (await update.ReadProblemAsync()).Code());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, delete.StatusCode);
    }

    [Fact]
    public async Task Delete_ARoleThatIsAssigned_Returns409()
    {
        var admin = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var role = await _team.CreateRoleAsync(admin, "Support", PermissionCodes.UserView);

        await _team.AssignRolesAsync(admin, member.Id, role.Id);

        var response = await _team.SendAsync(admin, HttpMethod.Delete, $"/api/roles/{role.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ErrorCodes.Roles.InUse, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task ARoleManager_CannotCreateARoleWithPermissionsTheyDoNotHave()
    {
        var admin = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var roleEditor = await _team.CreateRoleAsync(admin, "Role editor", PermissionCodes.UserView, PermissionCodes.RoleManage);

        await _team.AssignRolesAsync(admin, member.Id, roleEditor.Id);

        var allowed = await _team.SendAsync(member, HttpMethod.Post, "/api/roles", new SaveRoleRequest("Viewer", null, [PermissionCodes.UserView]));
        var escalation = await _team.SendAsync(member, HttpMethod.Post, "/api/roles", new SaveRoleRequest("Auditor", null, [PermissionCodes.AuditLogView]));

        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, escalation.StatusCode);
        Assert.Equal(ErrorCodes.InsufficientPrivileges, (await escalation.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task Update_ARoleOfAnotherCompany_Returns404()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();
        var foreignRole = await _team.CreateRoleAsync(adminB, "Foreign", PermissionCodes.UserView);

        var update = await _team.SendAsync(adminA, HttpMethod.Put, $"/api/roles/{foreignRole.Id}", new SaveRoleRequest("Taken over", null, []));
        var delete = await _team.SendAsync(adminA, HttpMethod.Delete, $"/api/roles/{foreignRole.Id}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }
}
