using Microsoft.AspNetCore.Authorization;
using OpsFlow.Application.Common.Security;

namespace OpsFlow.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUserPermissions _permissions;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(
        ICurrentUserPermissions permissions,
        IHttpContextAccessor httpContextAccessor)
    {
        _permissions = permissions;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        if (await _permissions.HasAsync(requirement.Permission, cancellationToken))
        {
            context.Succeed(requirement);
        }
    }
}
