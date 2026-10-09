using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateOnly? DueDate);

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate);

public sealed record AssignTaskRequest(Guid? AssigneeId);

public sealed record MoveTaskRequest(TaskItemStatus Status, int Position);

public sealed record AddTaskCommentRequest(string Body);
