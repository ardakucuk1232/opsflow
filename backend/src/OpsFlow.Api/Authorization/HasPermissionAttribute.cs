using Microsoft.AspNetCore.Authorization;

namespace OpsFlow.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new PermissionRequirement(Permission);
    }
}
