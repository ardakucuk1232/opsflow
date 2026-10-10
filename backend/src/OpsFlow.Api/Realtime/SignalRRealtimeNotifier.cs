using Microsoft.AspNetCore.SignalR;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Features.Notifications;

namespace OpsFlow.Api.Realtime;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<NotificationsHub> hub)
    {
        _hub = hub;
    }

    public Task NotificationCreatedAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken) =>
        _hub.Clients.User(userId.ToString()).SendAsync(NotificationsHub.NotificationEvent, notification, cancellationToken);

    public Task TaskChangedAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken) =>
        _hub.Clients.Group(NotificationsHub.ProjectGroup(projectId))
            .SendAsync(NotificationsHub.TaskChangedEvent, new { projectId, taskId }, cancellationToken);
}
