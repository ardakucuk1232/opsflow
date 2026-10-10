using OpsFlow.Application.Features.Notifications;

namespace OpsFlow.Application.Common.Interfaces;

public interface IRealtimeNotifier
{
    Task NotificationCreatedAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken);

    Task TaskChangedAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken);
}
