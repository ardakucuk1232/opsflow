using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Features.Attachments;
using OpsFlow.Application.Features.AuditLogs;
using OpsFlow.Application.Features.Notifications;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Application.Features.Reports;
using OpsFlow.Application.Features.Roles;
using OpsFlow.Application.Features.Search;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Application.Features.Users;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Security;

[Collection(ApiCollection.Name)]
public class CrossTenantAccessTests
{
    private readonly HttpClient _client;
    private readonly TeamScenario _team;

    public CrossTenantAccessTests(OpsFlowApiFactory factory)
    {
        _client = factory.CreateApiClient();
        _team = new TeamScenario(factory, _client);
    }

    [Fact]
    public async Task AnAdminOfAnotherCompany_CannotReachAnyRecordById()
    {
        var owner = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(owner, SystemRoles.Employee);
        var role = await _team.CreateRoleAsync(owner, "Gizli rol", PermissionCodes.UserView);
        var project = await _team.CreateProjectAsync(owner, "SEC");
        await _team.AddProjectMemberAsync(owner, project.Id, member.Id);
        var task = await _team.CreateTaskAsync(owner, project.Id, "Gizli görev", member.Id);
        var comment = await ReadAsync<TaskCommentDto>(await _team.SendAsync(owner, HttpMethod.Post, $"/api/tasks/{task.Id}/comments", new AddTaskCommentRequest("Gizli yorum")));
        var attachment = await ReadAsync<AttachmentDto>(await UploadAsync(owner, task.Id));
        var notification = (await _team.GetAsync<NotificationListDto>(member, "/api/notifications")).Items[0];

        var intruder = await _team.RegisterAdminAsync();
        var intruderRole = await _team.RoleIdAsync(intruder, SystemRoles.Employee);

        var requests = new (HttpMethod Method, string Path, object? Body)[]
        {
            (HttpMethod.Get, $"/api/projects/{project.Id}", null),
            (HttpMethod.Put, $"/api/projects/{project.Id}", new UpdateProjectRequest("Ele geçirildi", null, ProjectStatus.Active, null, null)),
            (HttpMethod.Delete, $"/api/projects/{project.Id}", null),
            (HttpMethod.Post, $"/api/projects/{project.Id}/members", new AddProjectMemberRequest(intruder.Id, ProjectMemberRole.Lead)),
            (HttpMethod.Put, $"/api/projects/{project.Id}/members/{member.Id}", new UpdateProjectMemberRequest(ProjectMemberRole.Lead)),
            (HttpMethod.Delete, $"/api/projects/{project.Id}/members/{member.Id}", null),
            (HttpMethod.Get, $"/api/projects/{project.Id}/tasks", null),
            (HttpMethod.Post, $"/api/projects/{project.Id}/tasks", new CreateTaskRequest("Sızma", null, TaskItemStatus.Todo, TaskPriority.Low, null, null)),
            (HttpMethod.Get, $"/api/tasks/{task.Id}", null),
            (HttpMethod.Put, $"/api/tasks/{task.Id}", new UpdateTaskRequest("Ele geçirildi", null, TaskPriority.Low, null)),
            (HttpMethod.Put, $"/api/tasks/{task.Id}/assignee", new AssignTaskRequest(null)),
            (HttpMethod.Post, $"/api/tasks/{task.Id}/move", new MoveTaskRequest(TaskItemStatus.Done, 0)),
            (HttpMethod.Delete, $"/api/tasks/{task.Id}", null),
            (HttpMethod.Get, $"/api/tasks/{task.Id}/comments", null),
            (HttpMethod.Post, $"/api/tasks/{task.Id}/comments", new AddTaskCommentRequest("Sızma")),
            (HttpMethod.Delete, $"/api/tasks/{task.Id}/comments/{comment.Id}", null),
            (HttpMethod.Get, $"/api/tasks/{task.Id}/attachments", null),
            (HttpMethod.Get, $"/api/attachments/{attachment.Id}/content", null),
            (HttpMethod.Delete, $"/api/attachments/{attachment.Id}", null),
            (HttpMethod.Post, $"/api/notifications/{notification.Id}/read", null),
            (HttpMethod.Get, $"/api/users/{member.Id}", null),
            (HttpMethod.Put, $"/api/users/{member.Id}/roles", new UpdateUserRolesRequest([intruderRole])),
            (HttpMethod.Post, $"/api/users/{member.Id}/deactivate", null),
            (HttpMethod.Post, $"/api/users/{member.Id}/resend-invitation", null),
            (HttpMethod.Put, $"/api/roles/{role.Id}", new SaveRoleRequest("Ele geçirildi", null, [PermissionCodes.UserView])),
            (HttpMethod.Delete, $"/api/roles/{role.Id}", null)
        };

        var failures = new List<string>();

        foreach (var (method, path, body) in requests)
        {
            var response = await _team.SendAsync(intruder, method, path, body);

            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                failures.Add($"{method} {path} -> {(int)response.StatusCode}");
            }
        }

        var upload = await UploadAsync(intruder, task.Id);

        if (upload.StatusCode != HttpStatusCode.NotFound)
        {
            failures.Add($"POST /api/tasks/{task.Id}/attachments -> {(int)upload.StatusCode}");
        }

        Assert.Empty(failures);

        var after = await _team.GetAsync<TaskDetailDto>(owner, $"/api/tasks/{task.Id}");
        Assert.Equal(("Gizli görev", TaskItemStatus.Todo, member.Id), (after.Title, after.Status, after.Assignee?.Id));
        Assert.Single(await _team.GetAsync<List<TaskCommentDto>>(owner, $"/api/tasks/{task.Id}/comments"));
        Assert.Single(await _team.GetAsync<List<AttachmentDto>>(owner, $"/api/tasks/{task.Id}/attachments"));
        Assert.Equal(2, (await _team.GetAsync<ProjectDetailDto>(owner, $"/api/projects/{project.Id}")).Members.Count);
        Assert.True((await _team.GetAsync<UserSummaryDto>(owner, $"/api/users/{member.Id}")).IsActive);
        Assert.False((await _team.GetAsync<NotificationListDto>(member, "/api/notifications")).Items[0].IsRead);
    }

    [Fact]
    public async Task ListsAndSearches_NeverIncludeAnotherCompanysRecords()
    {
        var owner = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(owner, "LEAK");
        var task = await _team.CreateTaskAsync(owner, project.Id, "Sızıntı testi");
        var intruder = await _team.RegisterAdminAsync();

        var projects = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(intruder, "/api/projects?pageSize=100");
        var tasks = await _team.GetAsync<PagedResult<TaskListItemDto>>(intruder, $"/api/tasks?projectId={project.Id}");
        var search = await _team.GetAsync<SearchResultDto>(intruder, "/api/search?q=sızıntı");
        var users = await _team.GetAsync<PagedResult<UserSummaryDto>>(intruder, "/api/users?pageSize=100");
        var report = await _team.GetAsync<ReportOverviewDto>(intruder, $"/api/reports/overview?projectId={project.Id}");
        var audit = await _team.GetAsync<PagedResult<AuditLogDto>>(intruder, $"/api/audit-logs?entityId={task.Id}");

        Assert.DoesNotContain(projects.Items, p => p.Id == project.Id);
        Assert.Empty(tasks.Items);
        Assert.Empty(search.Tasks);
        Assert.Empty(search.Projects);
        Assert.DoesNotContain(users.Items, u => u.Id == owner.Id);
        Assert.Equal(0, report.Totals.OpenTasks);
        Assert.Empty(audit.Items);
    }

    private async Task<HttpResponseMessage> UploadAsync(Member actor, Guid taskId)
    {
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes("gizli"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        using var form = new MultipartFormDataContent { { file, "file", "gizli.txt" } };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{taskId}/attachments") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor.AccessToken);

        return await _client.SendAsync(request);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}");

        var body = await response.Content.ReadFromJsonAsync<T>(JsonDefaults.Options);
        Assert.NotNull(body);

        return body;
    }
}
