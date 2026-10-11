using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class TaskSearchEndpointsTests
{
    private readonly TeamScenario _team;

    public TaskSearchEndpointsTests(OpsFlowApiFactory factory)
    {
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task Search_MatchesTitlesWithoutCaringAboutCase_AndExactTaskKeys()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin, "PAY");
        await CreateAsync(admin, project.Id, "Ödeme sayfası");
        await CreateAsync(admin, project.Id, "Giriş ekranı");
        await CreateAsync(admin, project.Id, "Ödeme raporu");

        var byTitle = await ListAsync(admin, "search=%C3%96DEME");
        var byKey = await ListAsync(admin, "search=pay-2");

        Assert.Equal(["Ödeme raporu", "Ödeme sayfası"], byTitle.Items.Select(t => t.Title).Order());
        Assert.Equal("Giriş ekranı", Assert.Single(byKey.Items).Title);
        Assert.Equal("PAY-2", byKey.Items[0].Key);
    }

    [Fact]
    public async Task Search_TreatsWildcardsAsPlainText()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        await CreateAsync(admin, project.Id, "Kapsam %100");
        await CreateAsync(admin, project.Id, "Kapsam yarım");
        await CreateAsync(admin, project.Id, "snake_case isimler");

        var percent = await ListAsync(admin, "search=%25");
        var underscore = await ListAsync(admin, "search=e_c");

        Assert.Equal("Kapsam %100", Assert.Single(percent.Items).Title);
        Assert.Equal("snake_case isimler", Assert.Single(underscore.Items).Title);
    }

    [Fact]
    public async Task Filters_CanBeCombined()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var web = await _team.CreateProjectAsync(admin);
        var app = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, web.Id, employee.Id);

        var urgent = await CreateAsync(admin, web.Id, "Acil", TaskPriority.Critical, employee.Id, new DateOnly(2026, 11, 1));
        var later = await CreateAsync(admin, web.Id, "Sonra", TaskPriority.Low, null, new DateOnly(2026, 12, 15));
        var done = await CreateAsync(admin, web.Id, "Bitti", TaskPriority.High, employee.Id, new DateOnly(2026, 11, 5));
        var other = await CreateAsync(admin, app.Id, "Diğer proje", TaskPriority.High);
        await _team.SendAsync(admin, HttpMethod.Post, $"/api/tasks/{done.Id}/move", new MoveTaskRequest(TaskItemStatus.Done, 0));

        var inWeb = await ListAsync(admin, $"projectId={web.Id}");
        var open = await ListAsync(admin, $"projectId={web.Id}&status=Todo&status=InProgress");
        var highOrCritical = await ListAsync(admin, "priority=High&priority=Critical");
        var assigned = await ListAsync(admin, $"assigneeId={employee.Id}");
        var unassigned = await ListAsync(admin, $"projectId={web.Id}&unassigned=true");
        var november = await ListAsync(admin, "dueFrom=2026-11-01&dueTo=2026-11-30");

        Assert.Equal(3, inWeb.TotalCount);
        Assert.Equal(Ids(urgent, later), Ids(open.Items));
        Assert.Equal(Ids(urgent, done, other), Ids(highOrCritical.Items));
        Assert.Equal(Ids(urgent, done), Ids(assigned.Items));
        Assert.Equal(Ids(later), Ids(unassigned.Items));
        Assert.Equal(Ids(urgent, done), Ids(november.Items));
    }

    [Fact]
    public async Task Sorting_AndPaging_Work()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        await CreateAsync(admin, project.Id, "C", TaskPriority.Low, null, new DateOnly(2026, 12, 1));
        await CreateAsync(admin, project.Id, "A", TaskPriority.Critical, null, null);
        await CreateAsync(admin, project.Id, "B", TaskPriority.Medium, null, new DateOnly(2026, 10, 20));

        var byDue = await ListAsync(admin, $"projectId={project.Id}&sort=DueDate");
        var byPriority = await ListAsync(admin, $"projectId={project.Id}&sort=Priority");
        var newest = await ListAsync(admin, $"projectId={project.Id}&sort=Newest");
        var secondPage = await ListAsync(admin, $"projectId={project.Id}&sort=Oldest&page=2&pageSize=2");

        Assert.Equal(["B", "C", "A"], byDue.Items.Select(t => t.Title));
        Assert.Equal(["A", "B", "C"], byPriority.Items.Select(t => t.Title));
        Assert.Equal(["B", "A", "C"], newest.Items.Select(t => t.Title));
        Assert.Equal("B", Assert.Single(secondPage.Items).Title);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(2, secondPage.TotalPages);
    }

    [Fact]
    public async Task OnlyTasksOfVisibleProjects_AreListed()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var otherAdmin = await _team.RegisterAdminAsync();
        var mine = await _team.CreateProjectAsync(admin);
        var hidden = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, mine.Id, employee.Id);
        var visible = await CreateAsync(admin, mine.Id, "Görünür iş");
        await CreateAsync(admin, hidden.Id, "Gizli iş");

        var asEmployee = await ListAsync(employee, "search=i%C5%9F");
        var hiddenProject = await ListAsync(employee, $"projectId={hidden.Id}");
        var asOtherCompany = await ListAsync(otherAdmin, $"projectId={mine.Id}");

        Assert.Equal(Ids(visible), Ids(asEmployee.Items));
        Assert.Empty(hiddenProject.Items);
        Assert.Empty(asOtherCompany.Items);
    }

    [Theory]
    [InlineData("unassigned=true&assigneeId=0b0f6f0e-6f0a-4a57-9d0e-3f0f8f1f2a01")]
    [InlineData("dueFrom=2026-12-01&dueTo=2026-11-01")]
    [InlineData("status=Unknown")]
    public async Task InvalidFilters_Return400(string queryString)
    {
        var admin = await _team.RegisterAdminAsync();

        var response = await _team.SendAsync(admin, HttpMethod.Get, $"/api/tasks?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<TaskDetailDto> CreateAsync(
        Member actor,
        Guid projectId,
        string title,
        TaskPriority priority = TaskPriority.Medium,
        Guid? assigneeId = null,
        DateOnly? dueDate = null)
    {
        var response = await _team.SendAsync(actor, HttpMethod.Post, $"/api/projects/{projectId}/tasks",
            new CreateTaskRequest(title, null, TaskItemStatus.Todo, priority, assigneeId, dueDate));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskDetailDto>(JsonDefaults.Options);
        Assert.NotNull(task);

        return task;
    }

    private Task<PagedResult<TaskListItemDto>> ListAsync(Member actor, string queryString) =>
        _team.GetAsync<PagedResult<TaskListItemDto>>(actor, $"/api/tasks?{queryString}");

    private static Guid[] Ids(params TaskDetailDto[] tasks) => tasks.Select(t => t.Id).Order().ToArray();

    private static Guid[] Ids(IEnumerable<TaskListItemDto> tasks) => tasks.Select(t => t.Id).Order().ToArray();
}
