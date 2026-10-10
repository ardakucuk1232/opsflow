using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt,
    string? Link);

public sealed record NotificationListDto(IReadOnlyList<NotificationDto> Items, int UnreadCount);
