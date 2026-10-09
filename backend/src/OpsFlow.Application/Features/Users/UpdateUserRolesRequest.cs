namespace OpsFlow.Application.Features.Users;

public sealed record UpdateUserRolesRequest(IReadOnlyCollection<Guid> RoleIds);
