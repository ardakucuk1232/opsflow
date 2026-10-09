namespace OpsFlow.Application.Features.Roles;

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    IReadOnlyCollection<string> Permissions,
    int UserCount);
