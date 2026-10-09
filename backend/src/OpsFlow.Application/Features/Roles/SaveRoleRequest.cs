namespace OpsFlow.Application.Features.Roles;

public sealed record SaveRoleRequest(
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions);
