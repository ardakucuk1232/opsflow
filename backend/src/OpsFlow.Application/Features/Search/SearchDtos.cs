using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Search;

public sealed record ProjectHitDto(Guid Id, string Key, string Name, ProjectStatus Status);

public sealed record TaskHitDto(Guid Id, Guid ProjectId, string ProjectName, string Key, string Title, TaskItemStatus Status);

public sealed record SearchResultDto(IReadOnlyList<ProjectHitDto> Projects, IReadOnlyList<TaskHitDto> Tasks);
