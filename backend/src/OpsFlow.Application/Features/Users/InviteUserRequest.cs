namespace OpsFlow.Application.Features.Users;

public sealed record InviteUserRequest(
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<Guid> RoleIds);
