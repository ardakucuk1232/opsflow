using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Exceptions;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class TaskEndpointsTests
{
    private readonly TeamScenario _team;

    public TaskEndpointsTests(OpsFlowApiFactory factory)
    {
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task Create_NumbersTasksPerProject()
    {
        var admin = await _team.RegisterAdminAsync();
        var web = await _team.CreateProjectAsync(admin, "WEB");
        var app = await _team.CreateProjectAsync(admin, "APP");

        var first = await CreateTaskAsync(admin, web.Id, "Ana sayfa tasarımı");
        var second = await CreateTaskAsync(admin, web.Id, "Giriş sayfası");
        var other = await CreateTaskAsync(admin, app.Id, "Mağaza sayfası");

        Assert.Equal("WEB-1", first.Key);
        Assert.Equal("WEB-2", second.Key);
        Assert.Equal("APP-1", other.Key);
        Assert.Equal(admin.Id, first.Reporter.Id);
        Assert.Equal(TaskItemStatus.Todo, first.Status);
    }

    [Fact]
    public async Task Create_InParallel_GivesEveryTaskItsOwnNumber()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);

        var responses = await Task.WhenAll(Enumerable.Range(1, 10).Select(index =>
            _team.SendAsync(admin, HttpMethod.Post, $"/api/projects/{project.Id}/tasks", NewTask($"Task {index}"))));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        var board = await BoardAsync(admin, project.Id);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], board.Select(t => t.Number).Order());
    }

    [Fact]
    public async Task TheNumberOfADeletedTask_IsNotReused()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin, "OPS");

        await CreateTaskAsync(admin, project.Id, "One");
        var two = await CreateTaskAsync(admin, project.Id, "Two");
        await _team.SendAsync(admin, HttpMethod.Delete, $"/api/tasks/{two.Id}");

        var three = await CreateTaskAsync(admin, project.Id, "Three");
        var board = await BoardAsync(admin, project.Id);

        Assert.Equal("OPS-3", three.Key);
        Assert.DoesNotContain(board, task => task.Id == two.Id);
    }

    [Fact]
    public async Task Create_WithoutATitle_Returns400()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);

        var response = await _team.SendAsync(admin, HttpMethod.Post, $"/api/projects/{project.Id}/tasks", NewTask(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Move_ReordersTasksWithinAndAcrossColumns()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var a = await CreateTaskAsync(admin, project.Id, "A");
        var b = await CreateTaskAsync(admin, project.Id, "B");
        var c = await CreateTaskAsync(admin, project.Id, "C");

        await MoveAsync(admin, c.Id, TaskItemStatus.Todo, 0);
        Assert.Equal(["C", "A", "B"], Column(await BoardAsync(admin, project.Id), TaskItemStatus.Todo));

        var inProgress = await MoveAsync(admin, a.Id, TaskItemStatus.InProgress, 0);
        var board = await BoardAsync(admin, project.Id);

        Assert.Equal(TaskItemStatus.InProgress, inProgress.Status);
        Assert.Equal(["C", "B"], Column(board, TaskItemStatus.Todo));
        Assert.Equal(["A"], Column(board, TaskItemStatus.InProgress));

        await MoveAsync(admin, b.Id, TaskItemStatus.InProgress, 99);
        Assert.Equal(["A", "B"], Column(await BoardAsync(admin, project.Id), TaskItemStatus.InProgress));
    }

    [Fact]
    public async Task MovingToDone_RecordsTheCompletionTime_AndMovingBackClearsIt()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var task = await CreateTaskAsync(admin, project.Id, "Finish me");

        var done = await MoveAsync(admin, task.Id, TaskItemStatus.Done, 0);
        var reopened = await MoveAsync(admin, task.Id, TaskItemStatus.Todo, 0);

        Assert.NotNull(done.CompletedAt);
        Assert.Null(reopened.CompletedAt);
    }

    [Fact]
    public async Task Assign_OnlyAcceptsActiveMembersOfTheProject()
    {
        var admin = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var outsider = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, member.Id);
        var task = await CreateTaskAsync(admin, project.Id, "Assign me");

        var toOutsider = await _team.SendAsync(admin, HttpMethod.Put, $"/api/tasks/{task.Id}/assignee", new AssignTaskRequest(outsider.Id));
        var toMember = await _team.SendAsync(admin, HttpMethod.Put, $"/api/tasks/{task.Id}/assignee", new AssignTaskRequest(member.Id));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, toOutsider.StatusCode);
        Assert.Equal(ErrorCodes.Tasks.AssigneeNotMember, (await toOutsider.ReadProblemAsync()).Code());
        Assert.Equal(HttpStatusCode.OK, toMember.StatusCode);

        var assigned = await _team.GetAsync<TaskDetailDto>(admin, $"/api/tasks/{task.Id}");
        Assert.Equal(member.Id, assigned.Assignee?.Id);

        var unassign = await _team.SendAsync(admin, HttpMethod.Put, $"/api/tasks/{task.Id}/assignee", new AssignTaskRequest(null));
        Assert.Equal(HttpStatusCode.OK, unassign.StatusCode);
        Assert.Null((await _team.GetAsync<TaskDetailDto>(admin, $"/api/tasks/{task.Id}")).Assignee);
    }

    [Fact]
    public async Task AnEmployee_CanUpdateAndMoveTasks_ButCannotCreateAssignOrDelete()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);
        var task = await CreateTaskAsync(admin, project.Id, "Shared task");

        var update = await _team.SendAsync(employee, HttpMethod.Put, $"/api/tasks/{task.Id}", new UpdateTaskRequest("Renamed", "Details", TaskPriority.High, new DateOnly(2026, 12, 1)));
        var move = await _team.SendAsync(employee, HttpMethod.Post, $"/api/tasks/{task.Id}/move", new MoveTaskRequest(TaskItemStatus.InProgress, 0));
        var create = await _team.SendAsync(employee, HttpMethod.Post, $"/api/projects/{project.Id}/tasks", NewTask("Mine"));
        var assign = await _team.SendAsync(employee, HttpMethod.Put, $"/api/tasks/{task.Id}/assignee", new AssignTaskRequest(employee.Id));
        var delete = await _team.SendAsync(employee, HttpMethod.Delete, $"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, assign.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);

        var updated = await _team.GetAsync<TaskDetailDto>(admin, $"/api/tasks/{task.Id}");
        Assert.Equal("Renamed", updated.Title);
        Assert.Equal(TaskPriority.High, updated.Priority);
        Assert.Equal(new DateOnly(2026, 12, 1), updated.DueDate);
        Assert.Equal(TaskItemStatus.InProgress, updated.Status);
    }

    [Fact]
    public async Task TasksOfAProjectTheUserIsNotIn_AreInvisible()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        var task = await CreateTaskAsync(admin, project.Id, "Secret");

        var board = await _team.SendAsync(employee, HttpMethod.Get, $"/api/projects/{project.Id}/tasks");
        var get = await _team.SendAsync(employee, HttpMethod.Get, $"/api/tasks/{task.Id}");
        var move = await _team.SendAsync(employee, HttpMethod.Post, $"/api/tasks/{task.Id}/move", new MoveTaskRequest(TaskItemStatus.Done, 0));
        var comments = await _team.SendAsync(employee, HttpMethod.Get, $"/api/tasks/{task.Id}/comments");

        Assert.Equal(HttpStatusCode.NotFound, board.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, move.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, comments.StatusCode);
    }

    [Fact]
    public async Task TasksOfAnotherCompany_AreInvisibleAndUntouchable()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(adminA);
        var task = await CreateTaskAsync(adminA, project.Id, "Company A task");

        var get = await _team.SendAsync(adminB, HttpMethod.Get, $"/api/tasks/{task.Id}");
        var update = await _team.SendAsync(adminB, HttpMethod.Put, $"/api/tasks/{task.Id}", new UpdateTaskRequest("Hijacked", null, TaskPriority.Low, null));
        var move = await _team.SendAsync(adminB, HttpMethod.Post, $"/api/tasks/{task.Id}/move", new MoveTaskRequest(TaskItemStatus.Done, 0));
        var delete = await _team.SendAsync(adminB, HttpMethod.Delete, $"/api/tasks/{task.Id}");
        var comment = await _team.SendAsync(adminB, HttpMethod.Post, $"/api/tasks/{task.Id}/comments", new AddTaskCommentRequest("Hello"));
        var create = await _team.SendAsync(adminB, HttpMethod.Post, $"/api/projects/{project.Id}/tasks", NewTask("Injected"));

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, move.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, comment.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);

        var unchanged = await _team.GetAsync<TaskDetailDto>(adminA, $"/api/tasks/{task.Id}");
        Assert.Equal("Company A task", unchanged.Title);
        Assert.Single(await BoardAsync(adminA, project.Id));
    }

    [Fact]
    public async Task Comments_CanBeDeletedByTheirAuthorOrSomeoneWhoCanDeleteTasks()
    {
        var admin = await _team.RegisterAdminAsync();
        var author = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var colleague = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, author.Id);
        await _team.AddProjectMemberAsync(admin, project.Id, colleague.Id);
        var task = await CreateTaskAsync(admin, project.Id, "Discuss");

        var first = await AddCommentAsync(author, task.Id, "  İlk yorum  ");
        var second = await AddCommentAsync(author, task.Id, "İkinci yorum");

        Assert.Equal("İlk yorum", first.Body);
        Assert.True(first.CanDelete);

        var asSeenByColleague = await _team.GetAsync<List<TaskCommentDto>>(colleague, $"/api/tasks/{task.Id}/comments");
        Assert.Equal(["İlk yorum", "İkinci yorum"], asSeenByColleague.Select(c => c.Body));
        Assert.All(asSeenByColleague, comment => Assert.False(comment.CanDelete));

        var byColleague = await _team.SendAsync(colleague, HttpMethod.Delete, $"/api/tasks/{task.Id}/comments/{first.Id}");
        var byAuthor = await _team.SendAsync(author, HttpMethod.Delete, $"/api/tasks/{task.Id}/comments/{first.Id}");
        var byAdmin = await _team.SendAsync(admin, HttpMethod.Delete, $"/api/tasks/{task.Id}/comments/{second.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, byColleague.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAuthor.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);

        var remaining = await _team.GetAsync<List<TaskCommentDto>>(admin, $"/api/tasks/{task.Id}/comments");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Board_CountsTheCommentsOfEachTask()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var task = await CreateTaskAsync(admin, project.Id, "Talk about it");

        await AddCommentAsync(admin, task.Id, "One");
        await AddCommentAsync(admin, task.Id, "Two");

        var board = await BoardAsync(admin, project.Id);
        Assert.Equal(2, Assert.Single(board).CommentCount);
    }

    [Fact]
    public async Task AssignedToMe_ListsOpenTasksAcrossProjects()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var web = await _team.CreateProjectAsync(admin, "WEB");
        var app = await _team.CreateProjectAsync(admin, "APP");
        await _team.AddProjectMemberAsync(admin, web.Id, employee.Id);
        await _team.AddProjectMemberAsync(admin, app.Id, employee.Id);

        var open = await CreateTaskAsync(admin, web.Id, "Open", employee.Id, new DateOnly(2026, 10, 20));
        var finished = await CreateTaskAsync(admin, web.Id, "Finished", employee.Id);
        var later = await CreateTaskAsync(admin, app.Id, "Later", employee.Id);
        await CreateTaskAsync(admin, app.Id, "Someone else's");
        await MoveAsync(admin, finished.Id, TaskItemStatus.Done, 0);

        var mine = await _team.GetAsync<List<AssignedTaskDto>>(employee, "/api/tasks/assigned-to-me");

        Assert.Equal([open.Id, later.Id], mine.Select(t => t.Id));
        Assert.Equal("WEB-1", mine[0].Key);
    }

    [Fact]
    public async Task LeavingAProject_UnassignsTheMembersTasks()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);
        var task = await CreateTaskAsync(admin, project.Id, "Handover", employee.Id);

        await _team.SendAsync(admin, HttpMethod.Delete, $"/api/projects/{project.Id}/members/{employee.Id}");

        var after = await _team.GetAsync<TaskDetailDto>(admin, $"/api/tasks/{task.Id}");
        var mine = await _team.GetAsync<List<AssignedTaskDto>>(employee, "/api/tasks/assigned-to-me");

        Assert.Null(after.Assignee);
        Assert.Empty(mine);
    }

    [Fact]
    public async Task DeletingAProject_HidesItsTasks()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var task = await CreateTaskAsync(admin, project.Id, "Orphan", admin.Id);

        await _team.SendAsync(admin, HttpMethod.Delete, $"/api/projects/{project.Id}");

        var get = await _team.SendAsync(admin, HttpMethod.Get, $"/api/tasks/{task.Id}");
        var mine = await _team.GetAsync<List<AssignedTaskDto>>(admin, "/api/tasks/assigned-to-me");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Empty(mine);
    }

    private async Task<TaskDetailDto> CreateTaskAsync(
        Member actor,
        Guid projectId,
        string title,
        Guid? assigneeId = null,
        DateOnly? dueDate = null)
    {
        var request = NewTask(title) with { AssigneeId = assigneeId, DueDate = dueDate };
        var response = await _team.SendAsync(actor, HttpMethod.Post, $"/api/projects/{projectId}/tasks", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskDetailDto>(JsonDefaults.Options);
        Assert.NotNull(task);

        return task;
    }

    private async Task<TaskDetailDto> MoveAsync(Member actor, Guid taskId, TaskItemStatus status, int position)
    {
        var response = await _team.SendAsync(actor, HttpMethod.Post, $"/api/tasks/{taskId}/move", new MoveTaskRequest(status, position));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskDetailDto>(JsonDefaults.Options);
        Assert.NotNull(task);

        return task;
    }

    private async Task<TaskCommentDto> AddCommentAsync(Member actor, Guid taskId, string body)
    {
        var response = await _team.SendAsync(actor, HttpMethod.Post, $"/api/tasks/{taskId}/comments", new AddTaskCommentRequest(body));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comment = await response.Content.ReadFromJsonAsync<TaskCommentDto>(JsonDefaults.Options);
        Assert.NotNull(comment);

        return comment;
    }

    private Task<List<TaskSummaryDto>> BoardAsync(Member actor, Guid projectId) =>
        _team.GetAsync<List<TaskSummaryDto>>(actor, $"/api/projects/{projectId}/tasks");

    private static string[] Column(IEnumerable<TaskSummaryDto> board, TaskItemStatus status) => board
        .Where(task => task.Status == status)
        .OrderBy(task => task.BoardOrder)
        .Select(task => task.Title)
        .ToArray();

    private static CreateTaskRequest NewTask(string title) =>
        new(title, null, TaskItemStatus.Todo, TaskPriority.Medium, null, null);
}
