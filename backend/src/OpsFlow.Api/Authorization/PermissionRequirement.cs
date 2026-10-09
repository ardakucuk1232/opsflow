using Microsoft.AspNetCore.Authorization;

namespace OpsFlow.Api.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
