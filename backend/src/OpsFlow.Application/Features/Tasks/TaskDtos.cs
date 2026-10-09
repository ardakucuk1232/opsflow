using OpsFlow.Application.Features.Projects;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Tasks;

public sealed record TaskSummaryDto(
    Guid Id,
    Guid ProjectId,
    string Key,
    int Number,
    string Title,
    TaskItemStatus Status,
    TaskPriority Priority,
    UserReferenceDto? Assignee,
    DateOnly? DueDate,
    int BoardOrder,
    int CommentCount,
    DateTimeOffset CreatedAt);

public sealed record TaskDetailDto(
    Guid Id,
    Guid ProjectId,
    string ProjectKey,
    string ProjectName,
    string Key,
    int Number,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    UserReferenceDto? Assignee,
    UserReferenceDto Reporter,
    DateOnly? DueDate,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AssignedTaskDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string Key,
    string Title,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate);

public sealed record TaskCommentDto(
    Guid Id,
    UserReferenceDto Author,
    string Body,
    DateTimeOffset CreatedAt,
    bool CanDelete);
