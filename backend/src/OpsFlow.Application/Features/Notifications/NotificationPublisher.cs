using Microsoft.Extensions.Logging;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Notifications;

public sealed record NotificationRequest(
    IEnumerable<Guid?> Recipients,
    Guid ActorId,
    NotificationType Type,
    string Title,
    string Message,
    string RelatedEntityType,
    Guid RelatedEntityId,
    string Link);

public sealed class NotificationPublisher
{
    private const int TitleLimit = 200;
    private const int MessageLimit = 1000;

    private readonly IOpsFlowDbContext _db;
    private readonly IRealtimeNotifier _realtime;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(IOpsFlowDbContext db, IRealtimeNotifier realtime, ILogger<NotificationPublisher> logger)
    {
        _db = db;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task PublishAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        var recipients = request.Recipients
            .OfType<Guid>()
            .Where(id => id != request.ActorId)
            .Distinct()
            .ToList();

        if (recipients.Count == 0)
        {
            return;
        }

        var notifications = recipients
            .Select(recipient => new Notification
            {
                RecipientUserId = recipient,
                Type = request.Type,
                Title = Truncate(request.Title, TitleLimit),
                Message = Truncate(request.Message, MessageLimit),
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId
            })
            .ToList();

        _db.Notifications.AddRange(notifications);

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            var dto = new NotificationDto(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                IsRead: false,
                notification.CreatedAt,
                request.Link);

            await PushAsync(() => _realtime.NotificationCreatedAsync(notification.RecipientUserId, dto, cancellationToken));
        }
    }

    public Task TaskChangedAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken) =>
        PushAsync(() => _realtime.TaskChangedAsync(projectId, taskId, cancellationToken));

    private async Task PushAsync(Func<Task> push)
    {
        try
        {
            await push();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Failed to push a realtime update.");
        }
    }

    private static string Truncate(string value, int limit) =>
        value.Length <= limit ? value : string.Concat(value.AsSpan(0, limit - 1), "…");
}
