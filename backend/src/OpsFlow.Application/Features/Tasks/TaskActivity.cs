using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Features.Notifications;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Tasks;

public sealed class TaskActivity
{
    private static readonly Dictionary<TaskItemStatus, string> StatusLabels = new()
    {
        [TaskItemStatus.Backlog] = "Bekleyen",
        [TaskItemStatus.Todo] = "Yapılacak",
        [TaskItemStatus.InProgress] = "Devam ediyor",
        [TaskItemStatus.InReview] = "İncelemede",
        [TaskItemStatus.Done] = "Tamamlandı",
        [TaskItemStatus.Cancelled] = "İptal edildi"
    };

    private readonly IOpsFlowDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly NotificationPublisher _publisher;

    public TaskActivity(IOpsFlowDbContext db, ITenantContext tenantContext, NotificationPublisher publisher)
    {
        _db = db;
        _tenantContext = tenantContext;
        _publisher = publisher;
    }

    public Task ChangedAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken) =>
        _publisher.TaskChangedAsync(projectId, taskId, cancellationToken);

    public async Task AssignedAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var context = await LoadAsync(taskId, cancellationToken);

        if (context is null)
        {
            return;
        }

        await _publisher.PublishAsync(
            Request(context, [context.AssigneeId], NotificationType.TaskAssigned,
                "Size görev atandı",
                $"{context.ActorName}, {context.Label} görevini size atadı."),
            cancellationToken);
    }

    public async Task StatusChangedAsync(Guid taskId, TaskItemStatus status, CancellationToken cancellationToken)
    {
        var context = await LoadAsync(taskId, cancellationToken);

        if (context is null)
        {
            return;
        }

        await _publisher.PublishAsync(
            Request(context, [context.AssigneeId, context.ReporterId], NotificationType.TaskStatusChanged,
                "Görevin durumu değişti",
                $"{context.ActorName}, {context.Label} görevini \"{StatusLabels[status]}\" durumuna taşıdı."),
            cancellationToken);
    }

    public async Task CommentAddedAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var context = await LoadAsync(taskId, cancellationToken);

        if (context is null)
        {
            return;
        }

        await _publisher.PublishAsync(
            Request(context, [context.AssigneeId, context.ReporterId], NotificationType.TaskCommentAdded,
                "Göreve yorum yazıldı",
                $"{context.ActorName}, {context.Label} görevine yorum yazdı."),
            cancellationToken);
    }

    private NotificationRequest Request(
        TaskContext context,
        IEnumerable<Guid?> recipients,
        NotificationType type,
        string title,
        string message) =>
        new(recipients, context.ActorId, type, title, message,
            NotificationLinks.TaskEntity, context.TaskId, NotificationLinks.ForTask(context.ProjectId, context.TaskId));

    private async Task<TaskContext?> LoadAsync(Guid taskId, CancellationToken cancellationToken)
    {
        if (_tenantContext.UserId is not Guid actorId)
        {
            return null;
        }

        var task = await _db.TaskItems
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => new { t.Id, t.ProjectId, ProjectKey = t.Project.Key, t.Number, t.Title, t.AssigneeId, t.ReporterId })
            .SingleOrDefaultAsync(cancellationToken);

        var actor = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == actorId)
            .Select(u => u.FirstName + " " + u.LastName)
            .SingleOrDefaultAsync(cancellationToken);

        return task is null || actor is null
            ? null
            : new TaskContext(task.Id, task.ProjectId, $"{task.ProjectKey}-{task.Number} {task.Title}", task.AssigneeId, task.ReporterId, actorId, actor);
    }

    private sealed record TaskContext(
        Guid TaskId,
        Guid ProjectId,
        string Label,
        Guid? AssigneeId,
        Guid ReporterId,
        Guid ActorId,
        string ActorName);
}
