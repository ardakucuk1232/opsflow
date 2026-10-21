using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Features.AuditLogs;
using OpsFlow.Application.Features.Auth;
using OpsFlow.Application.Features.Roles;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class AuditLogEndpointsTests
{
    private readonly HttpClient _client;
    private readonly TeamScenario _team;

    public AuditLogEndpointsTests(OpsFlowApiFactory factory)
    {
        _client = factory.CreateApiClient();
        _team = new TeamScenario(factory, _client);
    }

    [Fact]
    public async Task TaskChanges_AreRecordedWithTheActorAndTheChangedFields()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin, "AUD");
        var task = await _team.CreateTaskAsync(admin, project.Id, "İlk başlık");

        await _team.SendAsync(admin, HttpMethod.Put, $"/api/tasks/{task.Id}", new UpdateTaskRequest("Yeni başlık", null, TaskPriority.High, null));
        await _team.SendAsync(admin, HttpMethod.Post, $"/api/tasks/{task.Id}/move", new MoveTaskRequest(TaskItemStatus.InProgress, 0));
        await _team.SendAsync(admin, HttpMethod.Delete, $"/api/tasks/{task.Id}");

        var logs = (await ListAsync(admin, $"entityName=TaskItem&entityId={task.Id}")).Items;

        Assert.Equal([AuditActions.Deleted, AuditActions.Updated, AuditActions.Updated, AuditActions.Created], logs.Select(l => l.Action));
        Assert.All(logs, log => Assert.Equal(admin.Id, log.User?.Id));

        var created = logs[3];
        Assert.Equal("İlk başlık", created.EntityLabel);
        Assert.Equal("İlk başlık", Value(created, "Title").New);

        var renamed = logs[2];
        Assert.Equal(["Priority", "Title"], renamed.Changes.Select(c => c.Field).Order());
        Assert.Equal(("İlk başlık", "Yeni başlık"), Value(renamed, "Title"));
        Assert.Equal(("Medium", "High"), Value(renamed, "Priority"));

        var moved = logs[1];
        Assert.Equal(("Todo", "InProgress"), Value(moved, "Status"));
        Assert.DoesNotContain(moved.Changes, c => c.Field == "BoardOrder");

        Assert.Equal("Yeni başlık", logs[0].EntityLabel);
    }

    [Fact]
    public async Task MovingATask_DoesNotLogTheReorderedNeighbours()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var first = await _team.CreateTaskAsync(admin, project.Id, "Bir");
        var second = await _team.CreateTaskAsync(admin, project.Id, "İki");

        await _team.SendAsync(admin, HttpMethod.Post, $"/api/tasks/{second.Id}/move", new MoveTaskRequest(TaskItemStatus.Todo, 0));

        var neighbour = await ListAsync(admin, $"entityName=TaskItem&entityId={first.Id}&action=Updated");
        var moved = await ListAsync(admin, $"entityName=TaskItem&entityId={second.Id}&action=Updated");

        Assert.Empty(neighbour.Items);
        Assert.Empty(moved.Items);
    }

    [Fact]
    public async Task Registration_RecordsTheNewCompanyRolesAndUser_WithoutSecrets()
    {
        var admin = await _team.RegisterAdminAsync();

        var logs = (await ListAsync(admin, "pageSize=100")).Items;

        Assert.Contains(logs, l => l is { EntityName: "Company", Action: AuditActions.Created });
        Assert.DoesNotContain(logs, l => l.EntityName == "RolePermission");

        var adminRole = Assert.Single(logs, l => l is { EntityName: "Role", EntityLabel: SystemRoles.Admin });
        var permissions = Value(adminRole, "Permissions").New;
        Assert.Contains(PermissionCodes.AuditLogView, permissions);

        var user = Assert.Single(logs, l => l is { EntityName: "User", Action: AuditActions.Created });
        Assert.Equal("***", Value(user, "PasswordHash").New);
        Assert.Equal(admin.Email, user.EntityLabel);
    }

    [Fact]
    public async Task LoggingIn_IsNotRecorded_ButAPasswordResetIs_WithoutTheHash()
    {
        var admin = await _team.RegisterAdminAsync();
        var before = (await ListAsync(admin, "entityName=User")).TotalCount;

        await _client.LoginAsync(admin.Email, admin.Password);
        Assert.Equal(before, (await ListAsync(admin, "entityName=User")).TotalCount);

        await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(admin.Email));
        var token = _team.LatestTokenSentTo(admin.Email);
        await _client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, "Another-password-2"));

        var login = await _client.LoginAsync(admin.Email, "Another-password-2");
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        var fresh = admin with { AccessToken = auth!.AccessToken };

        var reset = (await ListAsync(fresh, "entityName=User&action=Updated")).Items;
        var change = Assert.Single(reset).Changes.Single(c => c.Field == "PasswordHash");

        Assert.Equal("***", change.OldValue?.GetString());
        Assert.Equal("***", change.NewValue?.GetString());
    }

    [Fact]
    public async Task RolePermissionChanges_AreRecordedOneByOne()
    {
        var admin = await _team.RegisterAdminAsync();
        var role = await _team.CreateRoleAsync(admin, "Denetçi", PermissionCodes.UserView);

        await _team.SendAsync(admin, HttpMethod.Put, $"/api/roles/{role.Id}", new SaveRoleRequest("Denetçi", null, [PermissionCodes.ReportView]));

        var logs = (await ListAsync(admin, "entityName=RolePermission")).Items;

        Assert.Contains(logs, l => l is { Action: AuditActions.Created, EntityLabel: "Denetçi → report.view" });
        Assert.Contains(logs, l => l is { Action: AuditActions.Deleted, EntityLabel: "Denetçi → user.view" });
    }

    [Fact]
    public async Task OnlyUsersWithThePermission_CanReadTheLog_AndOnlyTheirCompanys()
    {
        var admin = await _team.RegisterAdminAsync();
        var manager = await _team.AddMemberAsync(admin, SystemRoles.Manager);
        var otherAdmin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);

        var asManager = await _team.SendAsync(manager, HttpMethod.Get, "/api/audit-logs");
        var asOther = await ListAsync(otherAdmin, $"entityName=Project&entityId={project.Id}");
        var all = await ListAsync(otherAdmin, "pageSize=100");

        Assert.Equal(HttpStatusCode.Forbidden, asManager.StatusCode);
        Assert.Empty(asOther.Items);
        Assert.DoesNotContain(all.Items, l => l.EntityLabel == admin.Email);
    }

    [Fact]
    public async Task Filters_AreValidated()
    {
        var admin = await _team.RegisterAdminAsync();

        var unknownEntity = await _team.SendAsync(admin, HttpMethod.Get, "/api/audit-logs?entityName=RefreshToken");
        var reversedDates = await _team.SendAsync(admin, HttpMethod.Get, "/api/audit-logs?from=2026-10-10&to=2026-10-01");

        Assert.Equal(HttpStatusCode.BadRequest, unknownEntity.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reversedDates.StatusCode);
    }

    private Task<PagedResult<AuditLogDto>> ListAsync(Member actor, string query) =>
        _team.GetAsync<PagedResult<AuditLogDto>>(actor, $"/api/audit-logs?{query}");

    private static (string? Old, string? New) Value(AuditLogDto log, string field)
    {
        var change = log.Changes.Single(c => c.Field == field);

        return (Text(change.OldValue), Text(change.NewValue));
    }

    private static string? Text(JsonElement? value) => value switch
    {
        null => null,
        { ValueKind: JsonValueKind.Null } => null,
        { ValueKind: JsonValueKind.String } element => element.GetString(),
        { } element => element.GetRawText()
    };
}
