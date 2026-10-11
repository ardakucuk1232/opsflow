using System.Net;
using OpsFlow.Application.Features.Roles;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class CacheInvalidationTests
{
    private readonly TeamScenario _team;

    public CacheInvalidationTests(OpsFlowApiFactory factory)
    {
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task ChangingARolesPermissions_TakesEffectOnTheNextRequest()
    {
        var admin = await _team.RegisterAdminAsync();
        var role = await _team.CreateRoleAsync(admin, "Planlayıcı", PermissionCodes.UserView, PermissionCodes.TaskCreate);
        var planner = await _team.AddMemberAsync(admin, "Planlayıcı");
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, planner.Id);

        Assert.Equal(HttpStatusCode.Created, (await CreateTaskAsync(planner, project.Id)).StatusCode);

        var update = await _team.SendAsync(admin, HttpMethod.Put, $"/api/roles/{role.Id}", new SaveRoleRequest(role.Name, null, [PermissionCodes.UserView]));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await CreateTaskAsync(planner, project.Id)).StatusCode);

        await _team.AssignRolesAsync(admin, planner.Id, await _team.RoleIdAsync(admin, SystemRoles.Manager));

        Assert.Equal(HttpStatusCode.Created, (await CreateTaskAsync(planner, project.Id)).StatusCode);
    }

    [Fact]
    public async Task ADeactivatedUser_LosesTheirPermissionsImmediately()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);
        var task = await _team.CreateTaskAsync(admin, project.Id, "Yorumlanacak");

        var before = await _team.SendAsync(employee, HttpMethod.Post, $"/api/tasks/{task.Id}/comments", new AddTaskCommentRequest("İlk"));
        Assert.Equal(HttpStatusCode.Created, before.StatusCode);

        await _team.SendAsync(admin, HttpMethod.Post, $"/api/users/{employee.Id}/deactivate");

        var after = await _team.SendAsync(employee, HttpMethod.Post, $"/api/tasks/{task.Id}/comments", new AddTaskCommentRequest("İkinci"));
        Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);
    }

    [Fact]
    public async Task TheBoard_ReflectsEveryChange_AfterItWasCached()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);
        var task = await _team.CreateTaskAsync(admin, project.Id, "Önbellek", employee.Id);

        Assert.Single(await BoardAsync(admin, project.Id));

        var second = await _team.CreateTaskAsync(admin, project.Id, "İkinci");
        Assert.Equal(2, (await BoardAsync(admin, project.Id)).Count);

        await _team.SendAsync(admin, HttpMethod.Post, $"/api/tasks/{task.Id}/move", new MoveTaskRequest(TaskItemStatus.Done, 0));
        Assert.Equal(TaskItemStatus.Done, (await BoardAsync(admin, project.Id)).Single(t => t.Id == task.Id).Status);

        await _team.SendAsync(employee, HttpMethod.Post, $"/api/tasks/{task.Id}/comments", new AddTaskCommentRequest("Bitti"));
        Assert.Equal(1, (await BoardAsync(employee, project.Id)).Single(t => t.Id == task.Id).CommentCount);

        await _team.SendAsync(admin, HttpMethod.Delete, $"/api/projects/{project.Id}/members/{employee.Id}");
        Assert.Null((await BoardAsync(admin, project.Id)).Single(t => t.Id == task.Id).Assignee);

        await _team.SendAsync(admin, HttpMethod.Delete, $"/api/tasks/{second.Id}");
        Assert.Equal(task.Id, Assert.Single(await BoardAsync(admin, project.Id)).Id);
    }

    [Fact]
    public async Task ACachedBoard_IsNotServedToSomeoneWhoCannotSeeTheProject()
    {
        var admin = await _team.RegisterAdminAsync();
        var outsider = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var otherAdmin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        await _team.CreateTaskAsync(admin, project.Id, "Gizli");

        await BoardAsync(admin, project.Id);

        var asOutsider = await _team.SendAsync(outsider, HttpMethod.Get, $"/api/projects/{project.Id}/tasks");
        var asOtherCompany = await _team.SendAsync(otherAdmin, HttpMethod.Get, $"/api/projects/{project.Id}/tasks");

        Assert.Equal(HttpStatusCode.NotFound, asOutsider.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, asOtherCompany.StatusCode);
    }

    private Task<HttpResponseMessage> CreateTaskAsync(Member actor, Guid projectId) =>
        _team.SendAsync(actor, HttpMethod.Post, $"/api/projects/{projectId}/tasks",
            new CreateTaskRequest("Plan", null, TaskItemStatus.Todo, TaskPriority.Medium, null, null));

    private Task<List<TaskSummaryDto>> BoardAsync(Member actor, Guid projectId) =>
        _team.GetAsync<List<TaskSummaryDto>>(actor, $"/api/projects/{projectId}/tasks");
}
