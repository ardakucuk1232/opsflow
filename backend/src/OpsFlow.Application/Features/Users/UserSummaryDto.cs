namespace OpsFlow.Application.Features.Users;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    bool IsEmailVerified,
    bool InvitationPending,
    IReadOnlyCollection<RoleReferenceDto> Roles,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);
