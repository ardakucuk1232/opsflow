using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Security;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Projects;

public sealed class ProjectAccess
{
    private readonly IOpsFlowDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserPermissions _permissions;

    public ProjectAccess(IOpsFlowDbContext db, ITenantContext tenantContext, ICurrentUserPermissions permissions)
    {
        _db = db;
        _tenantContext = tenantContext;
        _permissions = permissions;
    }

    public Guid CurrentUserId => _tenantContext.UserId
        ?? throw new UnauthorizedException("Authentication is required.");

    public Task<bool> CanViewAllAsync(CancellationToken cancellationToken) =>
        _permissions.HasAsync(PermissionCodes.ProjectViewAll, cancellationToken);

    public Task<bool> CanManageAllAsync(CancellationToken cancellationToken) =>
        _permissions.HasAsync(PermissionCodes.ProjectManage, cancellationToken);

    public async Task<IQueryable<Project>> VisibleProjectsAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        var projects = _db.Projects.AsQueryable();

        if (await CanViewAllAsync(cancellationToken))
        {
            return projects;
        }

        return projects.Where(p => p.Members.Any(m => m.UserId == userId));
    }

    public async Task EnsureVisibleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var projects = await VisibleProjectsAsync(cancellationToken);

        if (!await projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException("The project was not found.");
        }
    }

    public async Task EnsureCanManageAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await EnsureVisibleAsync(projectId, cancellationToken);

        if (await CanManageAllAsync(cancellationToken))
        {
            return;
        }

        var userId = CurrentUserId;

        var isLead = await _db.ProjectMembers.AnyAsync(
            m => m.ProjectId == projectId && m.UserId == userId && m.Role == ProjectMemberRole.Lead,
            cancellationToken);

        if (!isLead)
        {
            throw new ForbiddenException("Only project leads and project managers can change this project.");
        }
    }
}
