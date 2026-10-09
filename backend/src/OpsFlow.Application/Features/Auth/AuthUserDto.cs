namespace OpsFlow.Application.Features.Auth;

public sealed record AuthUserDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Email,
    bool IsEmailVerified,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);