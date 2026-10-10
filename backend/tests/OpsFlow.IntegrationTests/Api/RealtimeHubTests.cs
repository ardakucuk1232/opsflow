using Microsoft.AspNetCore.SignalR.Client;
using OpsFlow.Application.Features.Notifications;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.IntegrationTests.Fixtures;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class RealtimeHubTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly OpsFlowApiFactory _factory;
    private readonly TeamScenario _team;

    public RealtimeHubTests(OpsFlowApiFactory factory)
    {
        _factory = factory;
        _team = new TeamScenario(factory, factory.CreateApiClient());
    }

    [Fact]
    public async Task AUser_ReceivesTheirOwnNotificationsLive()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var colleague = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var project = await _team.CreateProjectAsync(admin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);
        await _team.AddProjectMemberAsync(admin, project.Id, colleague.Id);

        await using var employeeConnection = _factory.CreateHubConnection(employee.AccessToken);
        await using var colleagueConnection = _factory.CreateHubConnection(colleague.AccessToken);

        var received = new TaskCompletionSource<NotificationDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var leaked = false;

        employeeConnection.On<NotificationDto>("notification", n => received.TrySetResult(n));
        colleagueConnection.On<NotificationDto>("notification", _ => leaked = true);

        await employeeConnection.StartAsync();
        await colleagueConnection.StartAsync();

        var task = await _team.CreateTaskAsync(admin, project.Id, "Canlı bildirim", employee.Id);

        var notification = await received.Task.WaitAsync(Timeout);

        Assert.Equal(NotificationType.TaskAssigned, notification.Type);
        Assert.Contains(task.Key, notification.Message);
        Assert.False(leaked);
    }

    [Fact]
    public async Task WatchingAProject_DeliversTaskChanges_OnlyForVisibleProjects()
    {
        var admin = await _team.RegisterAdminAsync();
        var employee = await _team.AddMemberAsync(admin, SystemRoles.Employee);
        var otherAdmin = await _team.RegisterAdminAsync();
        var project = await _team.CreateProjectAsync(admin);
        var hidden = await _team.CreateProjectAsync(admin);
        var foreign = await _team.CreateProjectAsync(otherAdmin);
        await _team.AddProjectMemberAsync(admin, project.Id, employee.Id);

        await using var connection = _factory.CreateHubConnection(employee.AccessToken);

        var changed = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<TaskChanged>("taskChanged", payload => changed.TrySetResult(payload.TaskId));

        await connection.StartAsync();

        Assert.True(await connection.InvokeAsync<bool>("WatchProject", project.Id));
        Assert.False(await connection.InvokeAsync<bool>("WatchProject", hidden.Id));
        Assert.False(await connection.InvokeAsync<bool>("WatchProject", foreign.Id));

        var task = await _team.CreateTaskAsync(admin, project.Id, "Panoda görünsün");

        Assert.Equal(task.Id, await changed.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task Connecting_WithoutAValidToken_Fails()
    {
        await using var connection = _factory.CreateHubConnection("not-a-token");

        await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync());
    }

    private sealed record TaskChanged(Guid ProjectId, Guid TaskId);
}
