using System.Net;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Features.Roles;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;
using StackExchange.Redis;

namespace OpsFlow.IntegrationTests.Api;

public class RedisCacheTests : IClassFixture<RedisApiFactory>
{
    private readonly RedisApiFactory _factory;
    private readonly TeamScenario _team;

    public RedisCacheTests(RedisApiFactory factory)
    {
        _factory = factory;
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task TheBoard_IsKeptInRedis_AndDroppedWhenATaskChanges()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        await _team.CreateTaskAsync(admin, project.Id, "Redis");

        await BoardAsync(admin, project.Id);
        Assert.Single(Keys($"opsflow:board:*:{project.Id:N}"));

        await _team.CreateTaskAsync(admin, project.Id, "Yeni");
        Assert.Empty(Keys($"opsflow:board:*:{project.Id:N}"));

        Assert.Equal(2, (await BoardAsync(admin, project.Id)).Count);
    }

    [Fact]
    public async Task Permissions_AreKeptInRedis_AndDroppedWhenTheRoleChanges()
    {
        var admin = await _team.RegisterAdminAsync();
        var role = await _team.CreateRoleAsync(admin, "Raporcu", PermissionCodes.UserView, PermissionCodes.ProjectCreate);
        var member = await _team.AddMemberAsync(admin, "Raporcu");

        var allowed = await _team.SendAsync(member, HttpMethod.Get, "/api/users");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Single(Keys($"opsflow:permissions:{member.Id:N}"));

        await _team.SendAsync(admin, HttpMethod.Put, $"/api/roles/{role.Id}", new SaveRoleRequest(role.Name, null, [PermissionCodes.ProjectCreate]));
        Assert.Empty(Keys($"opsflow:permissions:{member.Id:N}"));

        var denied = await _team.SendAsync(member, HttpMethod.Get, "/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Health_ReportsRedisAsHealthy()
    {
        var response = await _factory.CreateApiClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private List<RedisKey> Keys(string pattern)
    {
        var redis = _factory.Services.GetRequiredService<IConnectionMultiplexer>();

        return redis.GetServers().SelectMany(server => server.Keys(pattern: pattern)).ToList();
    }

    private Task<List<TaskSummaryDto>> BoardAsync(Member actor, Guid projectId) =>
        _team.GetAsync<List<TaskSummaryDto>>(actor, $"/api/projects/{projectId}/tasks");
}

public class UnreachableRedisTests : IClassFixture<UnreachableRedisApiFactory>
{
    private readonly UnreachableRedisApiFactory _factory;
    private readonly TeamScenario _team;

    public UnreachableRedisTests(UnreachableRedisApiFactory factory)
    {
        _factory = factory;
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task TheApi_KeepsWorkingWithoutRedis_AndReportsItselfAsDegraded()
    {
        var admin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        await _team.CreateTaskAsync(admin, project.Id, "Redis yok");

        var board = await _team.GetAsync<List<TaskSummaryDto>>(admin, $"/api/projects/{project.Id}/tasks");
        var again = await _team.GetAsync<List<TaskSummaryDto>>(admin, $"/api/projects/{project.Id}/tasks");
        var health = await _factory.CreateApiClient().GetAsync("/health");

        Assert.Single(board);
        Assert.Single(again);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("Degraded", await health.Content.ReadAsStringAsync());
    }
}
