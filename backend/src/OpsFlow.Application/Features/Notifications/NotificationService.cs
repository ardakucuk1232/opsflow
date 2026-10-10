using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Notifications;

public sealed class NotificationService : INotificationService
{
    private const int ListLimit = 30;

    private readonly IOpsFlowDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public NotificationService(IOpsFlowDbContext db, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _db = db;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    private Guid CurrentUserId => _tenantContext.UserId
        ?? throw new UnauthorizedException("Authentication is required.");

    public async Task<NotificationListDto> ListAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        var rows = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(ListLimit)
            .Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                n.RelatedEntityType,
                n.RelatedEntityId,
                TaskProjectId = n.RelatedEntityType == NotificationLinks.TaskEntity
                    ? _db.TaskItems.Where(t => t.Id == n.RelatedEntityId).Select(t => (Guid?)t.ProjectId).FirstOrDefault()
                    : null
            })
            .ToListAsync(cancellationToken);

        var unreadCount = await _db.Notifications.CountAsync(
            n => n.RecipientUserId == userId && !n.IsRead,
            cancellationToken);

        var items = rows
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                LinkFor(n.RelatedEntityType, n.RelatedEntityId, n.TaskProjectId)))
            .ToList();

        return new NotificationListDto(items, unreadCount);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        var now = _timeProvider.GetUtcNow();

        var updated = await _db.Notifications
            .Where(n => n.Id == id && n.RecipientUserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, n => n.ReadAt ?? now)
                    .SetProperty(n => n.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);

        if (updated == 0)
        {
            throw new NotFoundException("The notification was not found.");
        }
    }

    public Task MarkAllAsReadAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        var now = _timeProvider.GetUtcNow();

        return _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, (DateTimeOffset?)now)
                    .SetProperty(n => n.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);
    }

    private static string? LinkFor(string? entityType, Guid? entityId, Guid? taskProjectId) => entityType switch
    {
        NotificationLinks.TaskEntity when entityId is Guid taskId && taskProjectId is Guid projectId =>
            NotificationLinks.ForTask(projectId, taskId),
        NotificationLinks.ProjectEntity when entityId is Guid projectId =>
            NotificationLinks.ForProject(projectId),
        _ => null
    };
}
