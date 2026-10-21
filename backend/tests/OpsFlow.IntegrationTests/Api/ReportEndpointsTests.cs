using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Features.Reports;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class ReportEndpointsTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly TeamScenario _team;

    public ReportEndpointsTests(OpsFlowApiFactory factory)
    {
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task Overview_SummarisesTheVisibleTasks()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var web = await _team.CreateProjectAsync(admin, "WEB");
        var app = await _team.CreateProjectAsync(admin, "APP");
        await _team.AddProjectMemberAsync(admin, web.Id, employee.Id);

        await CreateAsync(admin, web.Id, "Gecikmiş", TaskPriority.Critical, employee.Id, Today.AddDays(-2));
        await CreateAsync(admin, web.Id, "Açık", TaskPriority.Low, employee.Id, Today.AddDays(3));
        var done = await CreateAsync(admin, web.Id, "Biten", TaskPriority.High, null, Today.AddDays(-5));
        await CreateAsync(admin, app.Id, "Atanmamış", TaskPriority.High, null, null);
        await _team.SendAsync(admin, HttpMethod.Post, $"/api/tasks/{done.Id}/move", new MoveTaskRequest(TaskItemStatus.Done, 0));

        var report = await _team.GetAsync<ReportOverviewDto>(admin, $"/api/reports/overview?today={Today:yyyy-MM-dd}");

        Assert.Equal(new ReportTotalsDto(2, 3, 1, 1, 1), report.Totals);
        Assert.Equal(1, report.ByStatus.Single(s => s.Status == TaskItemStatus.Done).Count);
        Assert.Equal(3, report.ByStatus.Single(s => s.Status == TaskItemStatus.Todo).Count);
        Assert.Equal(1, report.OpenByPriority.Single(p => p.Priority == TaskPriority.High).Count);

        Assert.Equal(ReportService.WeekCount, report.Weekly.Count);
        Assert.Equal(4, report.Weekly[^1].Created);
        Assert.Equal(1, report.Weekly[^1].Completed);
        Assert.Equal(DayOfWeek.Monday, report.Weekly[0].WeekStart.DayOfWeek);

        var workload = Assert.Single(report.Workload);
        Assert.Equal((employee.Id, 2, 1), (workload.User.Id, workload.Open, workload.Overdue));

        var webProgress = report.Projects.Single(p => p.Id == web.Id);
        Assert.Equal((3, 1, 2, 1), (webProgress.Total, webProgress.Done, webProgress.Open, webProgress.Overdue));
        Assert.Equal(web.Id, report.Projects[0].Id);
    }

    [Fact]
    public async Task Overview_CanBeLimitedToOneProject()
    {
        var admin = await _team.RegisterAdminAsync();
        var web = await _team.CreateProjectAsync(admin);
        var app = await _team.CreateProjectAsync(admin);
        await CreateAsync(admin, web.Id, "Web", TaskPriority.Low, null, null);
        await CreateAsync(admin, app.Id, "App", TaskPriority.Low, null, null);

        var report = await _team.GetAsync<ReportOverviewDto>(admin, $"/api/reports/overview?projectId={web.Id}");

        Assert.Equal(1, report.Totals.OpenTasks);
        Assert.Equal(web.Id, Assert.Single(report.Projects).Id);
    }

    [Fact]
    public async Task Overview_RequiresTheReportPermission_AndHidesOtherCompanies()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var otherAdmin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        await CreateAsync(admin, project.Id, "Gizli", TaskPriority.Low, null, null);

        var asEmployee = await _team.SendAsync(employee, HttpMethod.Get, "/api/reports/overview");
        var asOther = await _team.GetAsync<ReportOverviewDto>(otherAdmin, $"/api/reports/overview?projectId={project.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, asEmployee.StatusCode);
        Assert.Equal(0, asOther.Totals.OpenTasks);
        Assert.Empty(asOther.Projects);
    }

    [Fact]
    public async Task MySummary_CountsTheUsersOwnTasks()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);

        await CreateAsync(admin, project.Id, "Dün", TaskPriority.Low, employee.Id, Today.AddDays(-1));
        await CreateAsync(admin, project.Id, "Yarın", TaskPriority.Low, employee.Id, Today.AddDays(1));
        await CreateAsync(admin, project.Id, "Gelecek ay", TaskPriority.Low, employee.Id, Today.AddDays(30));
        var finished = await CreateAsync(admin, project.Id, "Bitti", TaskPriority.Low, employee.Id, null);
        await CreateAsync(admin, project.Id, "Başkasının", TaskPriority.Low, null, Today.AddDays(-1));
        await _team.SendAsync(employee, HttpMethod.Post, $"/api/tasks/{finished.Id}/move", new MoveTaskRequest(TaskItemStatus.Done, 0));

        var summary = await _team.GetAsync<MySummaryDto>(employee, $"/api/reports/me?today={Today:yyyy-MM-dd}");

        Assert.Equal(new MySummaryDto(3, 1, 1, 1), summary);
    }

    private async Task<TaskDetailDto> CreateAsync(Member actor, Guid projectId, string title, TaskPriority priority, Guid? assigneeId, DateOnly? dueDate)
    {
        var response = await _team.SendAsync(actor, HttpMethod.Post, $"/api/projects/{projectId}/tasks",
            new CreateTaskRequest(title, null, TaskItemStatus.Todo, priority, assigneeId, dueDate));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<TaskDetailDto>(JsonDefaults.Options))!;
    }
}
