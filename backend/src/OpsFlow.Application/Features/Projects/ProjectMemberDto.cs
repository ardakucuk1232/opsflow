using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Projects;

public sealed record ProjectMemberDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    ProjectMemberRole Role,
    DateTimeOffset JoinedAt);
