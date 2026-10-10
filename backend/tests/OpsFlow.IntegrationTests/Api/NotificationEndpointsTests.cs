using System.Net;
using OpsFlow.Application.Features.Notifications;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class NotificationEndpointsTests
{
    private readonly TeamScenario _team;

    public NotificationEndpointsTests(OpsFlowApiFactory factory)
    {
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task TaskActivity_NotifiesTheAssigneeAndReporter_ButNeverTheActor()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);

        var task = await _team.CreateTaskAsync(admin, project.Id, "Rapor hazırla", employee.Id);
        await _team.SendAsync(admin, HttpMethod.Post, $"/api/tasks/{task.Id}/move", new MoveTaskRequest(TaskItemStatus.InProgress, 0));
        await _team.SendAsync(admin, HttpMethod.Post, $"/api/tasks/{task.Id}/comments", new AddTaskCommentRequest("Yarın bakalım"));
        await _team.SendAsync(employee, HttpMethod.Post, $"/api/tasks/{task.Id}/comments", new AddTaskCommentRequest("Tamam"));

        var forEmployee = await NotificationsAsync(employee);
        var forAdmin = await NotificationsAsync(admin);

        Assert.Equal(
            [NotificationType.TaskCommentAdded, NotificationType.TaskStatusChanged, NotificationType.TaskAssigned, NotificationType.ProjectMemberAdded],
            forEmployee.Items.Select(n => n.Type));
        Assert.Equal(4, forEmployee.UnreadCount);
        Assert.Equal($"/projects/{project.Id}?task={task.Id}", forEmployee.Items[0].Link);
        Assert.Equal($"/projects/{project.Id}", forEmployee.Items[^1].Link);
        Assert.Contains(task.Key, forEmployee.Items[2].Message);

        Assert.Equal(NotificationType.TaskCommentAdded, Assert.Single(forAdmin.Items).Type);
    }

    [Fact]
    public async Task MarkAsRead_AndMarkAllAsRead_UpdateTheUnreadCount()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);
        await _team.CreateTaskAsync(admin, project.Id, "One", employee.Id);
        await _team.CreateTaskAsync(admin, project.Id, "Two", employee.Id);

        var before = await NotificationsAsync(employee);
        Assert.Equal(3, before.UnreadCount);

        var read = await _team.SendAsync(employee, HttpMethod.Post, $"/api/notifications/{before.Items[0].Id}/read");
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);

        var afterOne = await NotificationsAsync(employee);
        Assert.Equal(2, afterOne.UnreadCount);
        Assert.True(afterOne.Items.Single(n => n.Id == before.Items[0].Id).IsRead);

        var readAll = await _team.SendAsync(employee, HttpMethod.Post, "/api/notifications/read-all");
        Assert.Equal(HttpStatusCode.NoContent, readAll.StatusCode);

        var afterAll = await NotificationsAsync(employee);
        Assert.Equal(0, afterAll.UnreadCount);
        Assert.All(afterAll.Items, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task SomeoneElsesNotification_CannotBeMarkedAsRead()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var colleague = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var otherAdmin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);

        var notification = (await NotificationsAsync(employee)).Items.Single();

        var byColleague = await _team.SendAsync(colleague, HttpMethod.Post, $"/api/notifications/{notification.Id}/read");
        var byOtherCompany = await _team.SendAsync(otherAdmin, HttpMethod.Post, $"/api/notifications/{notification.Id}/read");

        Assert.Equal(HttpStatusCode.NotFound, byColleague.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, byOtherCompany.StatusCode);
        Assert.Equal(1, (await NotificationsAsync(employee)).UnreadCount);
        Assert.Empty((await NotificationsAsync(colleague)).Items);
    }

    private Task<NotificationListDto> NotificationsAsync(Member actor) =>
        _team.GetAsync<NotificationListDto>(actor, "/api/notifications");
}
