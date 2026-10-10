namespace OpsFlow.Application.Features.Notifications;

public interface INotificationService
{
    Task<NotificationListDto> ListAsync(CancellationToken cancellationToken);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken);
}
