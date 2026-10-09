using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Projects;

public sealed class ProjectService : IProjectService
{
    private readonly IOpsFlowDbContext _db;
    private readonly ProjectAccess _access;
    private readonly IValidator<ProjectListQuery> _listValidator;
    private readonly IValidator<CreateProjectRequest> _createValidator;
    private readonly IValidator<UpdateProjectRequest> _updateValidator;
    private readonly IValidator<AddProjectMemberRequest> _addMemberValidator;
    private readonly IValidator<UpdateProjectMemberRequest> _updateMemberValidator;
    private readonly TimeProvider _timeProvider;

    public ProjectService(
        IOpsFlowDbContext db,
        ProjectAccess access,
        IValidator<ProjectListQuery> listValidator,
        IValidator<CreateProjectRequest> createValidator,
        IValidator<UpdateProjectRequest> updateValidator,
        IValidator<AddProjectMemberRequest> addMemberValidator,
        IValidator<UpdateProjectMemberRequest> updateMemberValidator,
        TimeProvider timeProvider)
    {
        _db = db;
        _access = access;
        _listValidator = listValidator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addMemberValidator = addMemberValidator;
        _updateMemberValidator = updateMemberValidator;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<ProjectSummaryDto>> ListAsync(ProjectListQuery query, CancellationToken cancellationToken)
    {
        await _listValidator.ValidateAndThrowAsync(query, cancellationToken);

        var userId = _access.CurrentUserId;
        var canManageAll = await _access.CanManageAllAsync(cancellationToken);
        var projects = (await _access.VisibleProjectsAsync(cancellationToken)).AsNoTracking();

        if (query.MemberOnly)
        {
            projects = projects.Where(p => p.Members.Any(m => m.UserId == userId));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();

            projects = projects.Where(p => p.Name.ToLower().Contains(term) || p.Key.ToLower().Contains(term));
        }

        if (query.Status is ProjectStatus status)
        {
            projects = projects.Where(p => p.Status == status);
        }

        return await projects
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Key,
                p.Name,
                p.Status,
                p.StartDate,
                p.EndDate,
                p.Members.Count,
                p.Members
                    .Where(m => m.Role == ProjectMemberRole.Lead)
                    .OrderBy(m => m.JoinedAt)
                    .Select(m => new UserReferenceDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.Email))
                    .FirstOrDefault(),
                p.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (ProjectMemberRole?)m.Role)
                    .FirstOrDefault(),
                canManageAll || p.Members.Any(m => m.UserId == userId && m.Role == ProjectMemberRole.Lead),
                p.CreatedAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<ProjectDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _access.CurrentUserId;
        var canManageAll = await _access.CanManageAllAsync(cancellationToken);
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        var project = await projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectDetailDto(
                p.Id,
                p.Key,
                p.Name,
                p.Description,
                p.Status,
                p.StartDate,
                p.EndDate,
                new UserReferenceDto(p.CreatedByUser.Id, p.CreatedByUser.FirstName, p.CreatedByUser.LastName, p.CreatedByUser.Email),
                p.Members
                    .OrderByDescending(m => m.Role)
                    .ThenBy(m => m.User.FirstName)
                    .ThenBy(m => m.User.LastName)
                    .Select(m => new ProjectMemberDto(
                        m.UserId,
                        m.User.FirstName,
                        m.User.LastName,
                        m.User.Email,
                        m.User.IsActive,
                        m.Role,
                        m.JoinedAt))
                    .ToList(),
                p.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (ProjectMemberRole?)m.Role)
                    .FirstOrDefault(),
                canManageAll || p.Members.Any(m => m.UserId == userId && m.Role == ProjectMemberRole.Lead),
                p.CreatedAt,
                p.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return project ?? throw new NotFoundException("The project was not found.");
    }

    public async Task<ProjectDetailDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var userId = _access.CurrentUserId;
        var key = request.Key.Trim().ToUpperInvariant();

        var keyTaken = await _db.Projects.AnyAsync(p => p.Key == key, cancellationToken);

        if (keyTaken)
        {
            throw new ConflictException(ErrorCodes.Projects.KeyTaken, "A project with this key already exists.");
        }

        var now = _timeProvider.GetUtcNow();

        var project = new Project
        {
            Name = request.Name.Trim(),
            Key = key,
            Description = NormalizeDescription(request.Description),
            Status = request.Status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedByUserId = userId
        };

        project.Members.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectMemberRole.Lead,
            JoinedAt = now
        });

        _db.Projects.Add(project);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDetailDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _access.EnsureCanManageAsync(id, cancellationToken);

        var project = await _db.Projects.SingleAsync(p => p.Id == id, cancellationToken);

        project.Name = request.Name.Trim();
        project.Description = NormalizeDescription(request.Description);
        project.Status = request.Status;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _access.EnsureVisibleAsync(id, cancellationToken);

        var project = await _db.Projects.SingleAsync(p => p.Id == id, cancellationToken);

        project.IsDeleted = true;
        project.DeletedAt = _timeProvider.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectDetailDto> AddMemberAsync(
        Guid id,
        AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        await _addMemberValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _access.EnsureCanManageAsync(id, cancellationToken);

        var userExists = await _db.Users.AnyAsync(u => u.Id == request.UserId && u.IsActive, cancellationToken);

        if (!userExists)
        {
            throw new BusinessRuleException(
                ErrorCodes.Projects.InvalidMember,
                "The user does not exist or is not active.");
        }

        var alreadyMember = await _db.ProjectMembers.AnyAsync(
            m => m.ProjectId == id && m.UserId == request.UserId,
            cancellationToken);

        if (alreadyMember)
        {
            throw new ConflictException(ErrorCodes.Projects.MemberExists, "The user is already a member of this project.");
        }

        _db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = id,
            UserId = request.UserId,
            Role = request.Role,
            JoinedAt = _timeProvider.GetUtcNow()
        });

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<ProjectDetailDto> UpdateMemberAsync(
        Guid id,
        Guid userId,
        UpdateProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        await _updateMemberValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _access.EnsureCanManageAsync(id, cancellationToken);

        var member = await LoadMemberAsync(id, userId, cancellationToken);

        if (member.Role == ProjectMemberRole.Lead && request.Role != ProjectMemberRole.Lead)
        {
            await EnsureAnotherLeadAsync(id, userId, cancellationToken);
        }

        member.Role = request.Role;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await _access.EnsureCanManageAsync(id, cancellationToken);

        var member = await LoadMemberAsync(id, userId, cancellationToken);

        if (member.Role == ProjectMemberRole.Lead)
        {
            await EnsureAnotherLeadAsync(id, userId, cancellationToken);
        }

        _db.ProjectMembers.Remove(member);

        await _db.SaveChangesAsync(cancellationToken);

        await _db.TaskItems
            .Where(t => t.ProjectId == id && t.AssigneeId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.AssigneeId, (Guid?)null)
                    .SetProperty(t => t.UpdatedAt, (DateTimeOffset?)_timeProvider.GetUtcNow()),
                cancellationToken);
    }

    private async Task<ProjectMember> LoadMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        return await _db.ProjectMembers.SingleOrDefaultAsync(
                m => m.ProjectId == projectId && m.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException("The user is not a member of this project.");
    }

    private async Task EnsureAnotherLeadAsync(Guid projectId, Guid excludedUserId, CancellationToken cancellationToken)
    {
        var anotherLead = await _db.ProjectMembers.AnyAsync(
            m => m.ProjectId == projectId
                && m.UserId != excludedUserId
                && m.Role == ProjectMemberRole.Lead
                && m.User.IsActive,
            cancellationToken);

        if (!anotherLead)
        {
            throw new BusinessRuleException(ErrorCodes.Projects.LastLead, "A project must keep at least one active lead.");
        }
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
