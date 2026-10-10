using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Security;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Tasks;

public sealed class TaskService : ITaskService
{
    private const int AssignedToMeLimit = 50;

    private readonly IOpsFlowDbContext _db;
    private readonly ProjectAccess _access;
    private readonly ICurrentUserPermissions _permissions;
    private readonly ITaskNumberGenerator _numbers;
    private readonly TaskActivity _activity;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<MoveTaskRequest> _moveValidator;
    private readonly TimeProvider _timeProvider;

    public TaskService(
        IOpsFlowDbContext db,
        ProjectAccess access,
        ICurrentUserPermissions permissions,
        ITaskNumberGenerator numbers,
        TaskActivity activity,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<MoveTaskRequest> moveValidator,
        TimeProvider timeProvider)
    {
        _db = db;
        _access = access;
        _permissions = permissions;
        _numbers = numbers;
        _activity = activity;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _moveValidator = moveValidator;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<TaskSummaryDto>> ListForProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await _access.EnsureVisibleAsync(projectId, cancellationToken);

        var tasks = await _db.TaskItems
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Status)
            .ThenBy(t => t.BoardOrder)
            .ThenBy(t => t.Number)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                ProjectKey = t.Project.Key,
                t.Number,
                t.Title,
                t.Status,
                t.Priority,
                Assignee = t.Assignee == null
                    ? null
                    : new UserReferenceDto(t.Assignee.Id, t.Assignee.FirstName, t.Assignee.LastName, t.Assignee.Email),
                t.DueDate,
                t.BoardOrder,
                CommentCount = t.Comments.Count(),
                t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return tasks
            .Select(t => new TaskSummaryDto(
                t.Id,
                t.ProjectId,
                $"{t.ProjectKey}-{t.Number}",
                t.Number,
                t.Title,
                t.Status,
                t.Priority,
                t.Assignee,
                TaskDates.FromStored(t.DueDate),
                t.BoardOrder,
                t.CommentCount,
                t.CreatedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<AssignedTaskDto>> ListAssignedToMeAsync(CancellationToken cancellationToken)
    {
        var userId = _access.CurrentUserId;
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        var tasks = await _db.TaskItems
            .AsNoTracking()
            .Where(t => t.AssigneeId == userId
                && t.Status != TaskItemStatus.Done
                && t.Status != TaskItemStatus.Cancelled
                && projects.Any(p => p.Id == t.ProjectId))
            .OrderBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Take(AssignedToMeLimit)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                ProjectName = t.Project.Name,
                ProjectKey = t.Project.Key,
                t.Number,
                t.Title,
                t.Status,
                t.Priority,
                t.DueDate
            })
            .ToListAsync(cancellationToken);

        return tasks
            .Select(t => new AssignedTaskDto(
                t.Id,
                t.ProjectId,
                t.ProjectName,
                $"{t.ProjectKey}-{t.Number}",
                t.Title,
                t.Status,
                t.Priority,
                TaskDates.FromStored(t.DueDate)))
            .ToList();
    }

    public async Task<TaskDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        var task = await _db.TaskItems
            .AsNoTracking()
            .Where(t => t.Id == id && projects.Any(p => p.Id == t.ProjectId))
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                ProjectKey = t.Project.Key,
                ProjectName = t.Project.Name,
                t.Number,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                Assignee = t.Assignee == null
                    ? null
                    : new UserReferenceDto(t.Assignee.Id, t.Assignee.FirstName, t.Assignee.LastName, t.Assignee.Email),
                Reporter = new UserReferenceDto(t.Reporter.Id, t.Reporter.FirstName, t.Reporter.LastName, t.Reporter.Email),
                t.DueDate,
                t.CompletedAt,
                t.CreatedAt,
                t.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("The task was not found.");

        return new TaskDetailDto(
            task.Id,
            task.ProjectId,
            task.ProjectKey,
            task.ProjectName,
            $"{task.ProjectKey}-{task.Number}",
            task.Number,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.Assignee,
            task.Reporter,
            TaskDates.FromStored(task.DueDate),
            task.CompletedAt,
            task.CreatedAt,
            task.UpdatedAt);
    }

    public async Task<TaskDetailDto> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _access.EnsureVisibleAsync(projectId, cancellationToken);

        if (request.AssigneeId is Guid assigneeId)
        {
            await EnsureCanAssignAsync(cancellationToken);
            await EnsureProjectMemberAsync(projectId, assigneeId, cancellationToken);
        }

        var now = _timeProvider.GetUtcNow();

        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = NormalizeDescription(request.Description),
            Status = request.Status,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            ReporterId = _access.CurrentUserId,
            DueDate = TaskDates.ToStored(request.DueDate),
            BoardOrder = await NextBoardOrderAsync(projectId, request.Status, cancellationToken),
            CompletedAt = request.Status == TaskItemStatus.Done ? now : null
        };

        task.Number = await _numbers.NextAsync(projectId, cancellationToken);

        _db.TaskItems.Add(task);

        await _db.SaveChangesAsync(cancellationToken);

        if (task.AssigneeId is not null)
        {
            await _activity.AssignedAsync(task.Id, cancellationToken);
        }

        await _activity.ChangedAsync(projectId, task.Id, cancellationToken);

        return await GetAsync(task.Id, cancellationToken);
    }

    public async Task<TaskDetailDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var task = await LoadVisibleTaskAsync(id, cancellationToken);

        task.Title = request.Title.Trim();
        task.Description = NormalizeDescription(request.Description);
        task.Priority = request.Priority;
        task.DueDate = TaskDates.ToStored(request.DueDate);

        await _db.SaveChangesAsync(cancellationToken);
        await _activity.ChangedAsync(task.ProjectId, task.Id, cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<TaskDetailDto> AssignAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await LoadVisibleTaskAsync(id, cancellationToken);

        if (request.AssigneeId is Guid assigneeId)
        {
            await EnsureProjectMemberAsync(task.ProjectId, assigneeId, cancellationToken);
        }

        var assigneeChanged = task.AssigneeId != request.AssigneeId;

        task.AssigneeId = request.AssigneeId;

        await _db.SaveChangesAsync(cancellationToken);

        if (assigneeChanged && request.AssigneeId is not null)
        {
            await _activity.AssignedAsync(task.Id, cancellationToken);
        }

        await _activity.ChangedAsync(task.ProjectId, task.Id, cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<TaskDetailDto> MoveAsync(Guid id, MoveTaskRequest request, CancellationToken cancellationToken)
    {
        await _moveValidator.ValidateAndThrowAsync(request, cancellationToken);

        var task = await LoadVisibleTaskAsync(id, cancellationToken);

        var column = await _db.TaskItems
            .Where(t => t.ProjectId == task.ProjectId && t.Status == request.Status && t.Id != task.Id)
            .OrderBy(t => t.BoardOrder)
            .ThenBy(t => t.Number)
            .ToListAsync(cancellationToken);

        column.Insert(Math.Min(request.Position, column.Count), task);

        for (var index = 0; index < column.Count; index++)
        {
            column[index].BoardOrder = index;
        }

        var statusChanged = task.Status != request.Status;

        if (statusChanged)
        {
            task.Status = request.Status;
            task.CompletedAt = request.Status == TaskItemStatus.Done ? _timeProvider.GetUtcNow() : null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (statusChanged)
        {
            await _activity.StatusChangedAsync(task.Id, request.Status, cancellationToken);
        }

        await _activity.ChangedAsync(task.ProjectId, task.Id, cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await LoadVisibleTaskAsync(id, cancellationToken);

        task.IsDeleted = true;
        task.DeletedAt = _timeProvider.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);
        await _activity.ChangedAsync(task.ProjectId, task.Id, cancellationToken);
    }

    private async Task<TaskItem> LoadVisibleTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        return await _db.TaskItems
            .Where(t => t.Id == id && projects.Any(p => p.Id == t.ProjectId))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("The task was not found.");
    }

    private async Task EnsureCanAssignAsync(CancellationToken cancellationToken)
    {
        if (!await _permissions.HasAsync(PermissionCodes.TaskAssign, cancellationToken))
        {
            throw new ForbiddenException("You do not have permission to assign tasks.");
        }
    }

    private async Task EnsureProjectMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var isMember = await _db.ProjectMembers.AnyAsync(
            m => m.ProjectId == projectId && m.UserId == userId && m.User.IsActive,
            cancellationToken);

        if (!isMember)
        {
            throw new BusinessRuleException(
                ErrorCodes.Tasks.AssigneeNotMember,
                "Tasks can only be assigned to active members of the project.");
        }
    }

    private async Task<int> NextBoardOrderAsync(Guid projectId, TaskItemStatus status, CancellationToken cancellationToken)
    {
        var highest = await _db.TaskItems
            .Where(t => t.ProjectId == projectId && t.Status == status)
            .MaxAsync(t => (int?)t.BoardOrder, cancellationToken);

        return (highest ?? -1) + 1;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
