using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Common.Security;

public sealed class PrivilegeGuard
{
    private readonly ICurrentUserPermissions _permissions;

    public PrivilegeGuard(ICurrentUserPermissions permissions)
    {
        _permissions = permissions;
    }

    public async Task EnsureHoldsAllAsync(IEnumerable<string> permissions, CancellationToken cancellationToken)
    {
        var own = await _permissions.GetAsync(cancellationToken);

        if (!permissions.All(own.Contains))
        {
            throw new ForbiddenException(
                ErrorCodes.InsufficientPrivileges,
                "You cannot grant or change permissions that you do not have yourself.");
        }
    }
}
