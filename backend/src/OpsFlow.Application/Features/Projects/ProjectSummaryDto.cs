using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Projects;

public sealed record ProjectSummaryDto(
    Guid Id,
    string Key,
    string Name,
    ProjectStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int MemberCount,
    UserReferenceDto? Lead,
    ProjectMemberRole? CurrentUserRole,
    bool CanManage,
    DateTimeOffset CreatedAt);
