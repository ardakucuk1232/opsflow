using Microsoft.AspNetCore.SignalR;
using OpsFlow.Application.Common.Security;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Infrastructure.Tenancy;

namespace OpsFlow.Api.Realtime;

public sealed class NotificationsHub : Hub
{
    public const string Path = "/hubs/notifications";
    public const string NotificationEvent = "notification";
    public const string TaskChangedEvent = "taskChanged";

    private readonly TenantContext _tenantContext;
    private readonly ProjectAccess _projectAccess;

    public NotificationsHub(TenantContext tenantContext, ProjectAccess projectAccess)
    {
        _tenantContext = tenantContext;
        _projectAccess = projectAccess;
    }

    public static string ProjectGroup(Guid projectId) => $"project:{projectId}";

    public async Task<bool> WatchProject(Guid projectId)
    {
        if (!TryResolveTenant())
        {
            return false;
        }

        var visible = await _projectAccess.VisibleProjectsAsync(Context.ConnectionAborted);

        if (!visible.Any(p => p.Id == projectId))
        {
            return false;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectId), Context.ConnectionAborted);

        return true;
    }

    public Task UnwatchProject(Guid projectId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(projectId), Context.ConnectionAborted);

    private bool TryResolveTenant()
    {
        var companyId = Context.User?.FindFirst(OpsFlowClaimTypes.CompanyId)?.Value;
        var userId = Context.User?.FindFirst(OpsFlowClaimTypes.UserId)?.Value;

        if (!Guid.TryParse(companyId, out var company) || !Guid.TryParse(userId, out var user))
        {
            return false;
        }

        _tenantContext.Set(company, user);

        return true;
    }
}
