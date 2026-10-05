using Microsoft.AspNetCore.Authorization;
using OpsFlow.Application.Common.Security;
using OpsFlow.Domain.Exceptions;
using OpsFlow.Infrastructure.Tenancy;

namespace OpsFlow.Api.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, TenantContext tenantContext)
    {
        if (ShouldResolveTenant(httpContext))
        {
            var companyIdValue = httpContext.User.FindFirst(OpsFlowClaimTypes.CompanyId)?.Value;
            var userIdValue = httpContext.User.FindFirst(OpsFlowClaimTypes.UserId)?.Value;

            if (!Guid.TryParse(companyIdValue, out var companyId) || !Guid.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedException("The acces token is missing required claims");
            }

            tenantContext.Set(companyId, userId);
        }

        await _next(httpContext);
    }

    public static bool ShouldResolveTenant(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var endpoint = httpContext.GetEndpoint();

        return endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is null;
    }
}