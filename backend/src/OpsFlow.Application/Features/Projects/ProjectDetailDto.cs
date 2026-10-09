using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Projects;

public sealed record ProjectDetailDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    UserReferenceDto CreatedBy,
    IReadOnlyCollection<ProjectMemberDto> Members,
    ProjectMemberRole? CurrentUserRole,
    bool CanManage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
