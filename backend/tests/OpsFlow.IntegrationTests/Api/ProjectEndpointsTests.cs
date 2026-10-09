using System.Net;
using System.Net.Http.Json;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Exceptions;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class ProjectEndpointsTests
{
    private readonly TeamScenario _team;

    public ProjectEndpointsTests(OpsFlowApiFactory factory)
    {
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task Create_MakesTheCreatorTheProjectLead()
    {
        var admin = await _team.RegisterAdminAsync();

        var project = await CreateProjectAsync(admin, key: "web");

        Assert.Equal("WEB", project.Key);
        Assert.Equal(ProjectStatus.Planning, project.Status);
        Assert.True(project.CanManage);
        Assert.Equal(ProjectMemberRole.Lead, project.CurrentUserRole);

        var member = Assert.Single(project.Members);
        Assert.Equal(admin.Id, member.UserId);
        Assert.Equal(ProjectMemberRole.Lead, member.Role);
        Assert.Equal(admin.Id, project.CreatedBy.Id);
    }

    [Fact]
    public async Task Status_IsSerializedAsText()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await CreateProjectAsync(admin);

        var response = await _team.SendAsync(admin, HttpMethod.Get, $"/api/projects/{project.Id}");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"status\":\"Planning\"", json);
        Assert.Contains("\"role\":\"Lead\"", json);
    }

    [Fact]
    public async Task Create_WithAKeyThatIsTaken_Returns409_UntilTheOtherProjectIsDeleted()
    {
        var admin = await _team.RegisterAdminAsync();
        var first = await CreateProjectAsync(admin, key: "OPS");

        var duplicate = await _team.SendAsync(admin, HttpMethod.Post, "/api/projects", NewProject(key: "ops"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(ErrorCodes.Projects.KeyTaken, (await duplicate.ReadProblemAsync()).Code());

        await _team.SendAsync(admin, HttpMethod.Delete, $"/api/projects/{first.Id}");

        var reused = await _team.SendAsync(admin, HttpMethod.Post, "/api/projects", NewProject(key: "OPS"));
        Assert.Equal(HttpStatusCode.Created, reused.StatusCode);
    }

    [Fact]
    public async Task TheSameKey_CanBeUsedByAnotherCompany()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();

        await CreateProjectAsync(adminA, key: "CRM");
        var other = await _team.SendAsync(adminB, HttpMethod.Post, "/api/projects", NewProject(key: "CRM"));

        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("1ABC")]
    [InlineData("AB-C")]
    [InlineData("ABCDEFGHIJK")]
    public async Task Create_WithAnInvalidKey_Returns400(string key)
    {
        var admin = await _team.RegisterAdminAsync();

        var response = await _team.SendAsync(admin, HttpMethod.Post, "/api/projects", NewProject(key: key));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAnEndDateBeforeTheStartDate_Returns400()
    {
        var admin = await _team.RegisterAdminAsync();
        var request = NewProject() with { StartDate = new DateOnly(2026, 11, 1), EndDate = new DateOnly(2026, 10, 1) };

        var response = await _team.SendAsync(admin, HttpMethod.Post, "/api/projects", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnEmployee_CannotCreateProjects()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);

        var response = await _team.SendAsync(employee, HttpMethod.Post, "/api/projects", NewProject());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnEmployee_SeesOnlyProjectsTheyAreAMemberOf()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var joined = await CreateProjectAsync(admin);
        var other = await CreateProjectAsync(admin);

        await AddMemberAsync(admin, joined.Id, employee.Id, ProjectMemberRole.Member);

        var list = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(employee, "/api/projects");
        var hidden = await _team.SendAsync(employee, HttpMethod.Get, $"/api/projects/{other.Id}");

        Assert.Equal(joined.Id, Assert.Single(list.Items).Id);
        Assert.Equal(ProjectMemberRole.Member, list.Items[0].CurrentUserRole);
        Assert.False(list.Items[0].CanManage);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task AManager_SeesEveryProjectOfTheCompany()
    {
        var admin = await _team.RegisterAdminAsync();
        var manager = await _team.AddMemberAsync(admin, SystemRoles.Manager);

        await CreateProjectAsync(admin);
        await CreateProjectAsync(admin);

        var all = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(manager, "/api/projects");
        var mine = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(manager, "/api/projects?memberOnly=true");

        Assert.Equal(2, all.TotalCount);
        Assert.All(all.Items, project => Assert.True(project.CanManage));
        Assert.Empty(mine.Items);
    }

    [Fact]
    public async Task ProjectsOfAnotherCompany_AreInvisibleAndUntouchable()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();
        var memberB = await _team.AddMemberAsync(adminB, SystemRoles.Employee);
        var project = await CreateProjectAsync(adminA);

        var list = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(adminB, "/api/projects");
        var get = await _team.SendAsync(adminB, HttpMethod.Get, $"/api/projects/{project.Id}");
        var update = await _team.SendAsync(adminB, HttpMethod.Put, $"/api/projects/{project.Id}", UpdateOf(project, name: "Taken over"));
        var delete = await _team.SendAsync(adminB, HttpMethod.Delete, $"/api/projects/{project.Id}");
        var join = await _team.SendAsync(adminB, HttpMethod.Post, $"/api/projects/{project.Id}/members", new AddProjectMemberRequest(memberB.Id, ProjectMemberRole.Lead));

        Assert.Empty(list.Items);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, join.StatusCode);

        var unchanged = await _team.GetAsync<ProjectDetailDto>(adminA, $"/api/projects/{project.Id}");
        Assert.Equal(project.Name, unchanged.Name);
        Assert.Single(unchanged.Members);
    }

    [Fact]
    public async Task AddingAUserOfAnotherCompany_Returns422()
    {
        var adminA = await _team.RegisterAdminAsync();
        var adminB = await _team.RegisterAdminAsync();
        var project = await CreateProjectAsync(adminA);

        var response = await _team.SendAsync(adminA, HttpMethod.Post, $"/api/projects/{project.Id}/members", new AddProjectMemberRequest(adminB.Id, ProjectMemberRole.Member));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ErrorCodes.Projects.InvalidMember, (await response.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task AddingTheSameMemberTwice_Returns409()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await CreateProjectAsync(admin);

        await AddMemberAsync(admin, project.Id, employee.Id, ProjectMemberRole.Member);
        var again = await _team.SendAsync(admin, HttpMethod.Post, $"/api/projects/{project.Id}/members", new AddProjectMemberRequest(employee.Id, ProjectMemberRole.Member));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(ErrorCodes.Projects.MemberExists, (await again.ReadProblemAsync()).Code());
    }

    [Fact]
    public async Task APlainMember_CannotChangeTheProject_ButALeadCan()
    {
        var admin = await _team.RegisterAdminAsync();
        var member = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var lead = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var newcomer = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await CreateProjectAsync(admin);

        await AddMemberAsync(admin, project.Id, member.Id, ProjectMemberRole.Member);
        await AddMemberAsync(admin, project.Id, lead.Id, ProjectMemberRole.Lead);

        var byMember = await _team.SendAsync(member, HttpMethod.Put, $"/api/projects/{project.Id}", UpdateOf(project, name: "Renamed"));
        var byLead = await _team.SendAsync(lead, HttpMethod.Put, $"/api/projects/{project.Id}", UpdateOf(project, name: "Renamed", status: ProjectStatus.Active));
        var leadAddsMember = await _team.SendAsync(lead, HttpMethod.Post, $"/api/projects/{project.Id}/members", new AddProjectMemberRequest(newcomer.Id, ProjectMemberRole.Member));
        var memberRemovesMember = await _team.SendAsync(member, HttpMethod.Delete, $"/api/projects/{project.Id}/members/{newcomer.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, byMember.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byLead.StatusCode);
        Assert.Equal(HttpStatusCode.OK, leadAddsMember.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, memberRemovesMember.StatusCode);

        var updated = await _team.GetAsync<ProjectDetailDto>(admin, $"/api/projects/{project.Id}");
        Assert.Equal("Renamed", updated.Name);
        Assert.Equal(ProjectStatus.Active, updated.Status);
        Assert.Equal(4, updated.Members.Count);
    }

    [Fact]
    public async Task TheLastLead_CannotBeRemovedOrDemoted()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await CreateProjectAsync(admin);

        await AddMemberAsync(admin, project.Id, employee.Id, ProjectMemberRole.Member);

        var demote = await _team.SendAsync(admin, HttpMethod.Put, $"/api/projects/{project.Id}/members/{admin.Id}", new UpdateProjectMemberRequest(ProjectMemberRole.Member));
        var remove = await _team.SendAsync(admin, HttpMethod.Delete, $"/api/projects/{project.Id}/members/{admin.Id}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, demote.StatusCode);
        Assert.Equal(ErrorCodes.Projects.LastLead, (await demote.ReadProblemAsync()).Code());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, remove.StatusCode);

        var promote = await _team.SendAsync(admin, HttpMethod.Put, $"/api/projects/{project.Id}/members/{employee.Id}", new UpdateProjectMemberRequest(ProjectMemberRole.Lead));
        var removeNow = await _team.SendAsync(admin, HttpMethod.Delete, $"/api/projects/{project.Id}/members/{admin.Id}");

        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, removeNow.StatusCode);
    }

    [Fact]
    public async Task Delete_RequiresProjectManagePermission_AndHidesTheProject()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await CreateProjectAsync(admin);

        await AddMemberAsync(admin, project.Id, employee.Id, ProjectMemberRole.Lead);

        var byLead = await _team.SendAsync(employee, HttpMethod.Delete, $"/api/projects/{project.Id}");
        var byAdmin = await _team.SendAsync(admin, HttpMethod.Delete, $"/api/projects/{project.Id}");
        var afterwards = await _team.SendAsync(admin, HttpMethod.Get, $"/api/projects/{project.Id}");
        var list = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(admin, "/api/projects");

        Assert.Equal(HttpStatusCode.Forbidden, byLead.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task List_FiltersBySearchAndStatus_AndPaginates()
    {
        var admin = await _team.RegisterAdminAsync();
        var mobile = await CreateProjectAsync(admin, name: "Mobil uygulama", key: "MOB");
        await CreateProjectAsync(admin, name: "Web sitesi", key: "WEB");
        var active = await CreateProjectAsync(admin, name: "Muhasebe", key: "ACC");

        await _team.SendAsync(admin, HttpMethod.Put, $"/api/projects/{active.Id}", UpdateOf(active, status: ProjectStatus.Active));

        var byName = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(admin, "/api/projects?search=mobil");
        var byKey = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(admin, "/api/projects?search=mob");
        var byStatus = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(admin, "/api/projects?status=Active");
        var firstPage = await _team.GetAsync<PagedResult<ProjectSummaryDto>>(admin, "/api/projects?pageSize=2");

        Assert.Equal(mobile.Id, Assert.Single(byName.Items).Id);
        Assert.Equal(mobile.Id, Assert.Single(byKey.Items).Id);
        Assert.Equal(active.Id, Assert.Single(byStatus.Items).Id);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(["Mobil uygulama", "Muhasebe"], firstPage.Items.Select(p => p.Name));
    }

    private async Task<ProjectDetailDto> CreateProjectAsync(Member actor, string? name = null, string? key = null)
    {
        var response = await _team.SendAsync(actor, HttpMethod.Post, "/api/projects", NewProject(name, key));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var project = await response.Content.ReadFromJsonAsync<ProjectDetailDto>(JsonDefaults.Options);
        Assert.NotNull(project);

        return project;
    }

    private async Task AddMemberAsync(Member actor, Guid projectId, Guid userId, ProjectMemberRole role)
    {
        var response = await _team.SendAsync(actor, HttpMethod.Post, $"/api/projects/{projectId}/members", new AddProjectMemberRequest(userId, role));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static CreateProjectRequest NewProject(string? name = null, string? key = null)
    {
        var unique = Guid.NewGuid().ToString("N");

        return new CreateProjectRequest(
            name ?? $"Project {unique[..8]}",
            key ?? $"P{unique[..6].ToUpperInvariant()}",
            null,
            ProjectStatus.Planning,
            null,
            null);
    }

    private static UpdateProjectRequest UpdateOf(ProjectDetailDto project, string? name = null, ProjectStatus? status = null) =>
        new(name ?? project.Name, project.Description, status ?? project.Status, project.StartDate, project.EndDate);
}
