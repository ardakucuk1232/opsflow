namespace OpsFlow.Application.Features.Auth;

public sealed record AuthUserDto(
    Guid Id,
    Guid CompanyId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles);