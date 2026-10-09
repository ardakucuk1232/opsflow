using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Security;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Tasks;

public sealed class TaskCommentService : ITaskCommentService
{
    private readonly IOpsFlowDbContext _db;
    private readonly ProjectAccess _access;
    private readonly ICurrentUserPermissions _permissions;
    private readonly IValidator<AddTaskCommentRequest> _validator;
    private readonly TimeProvider _timeProvider;

    public TaskCommentService(
        IOpsFlowDbContext db,
        ProjectAccess access,
        ICurrentUserPermissions permissions,
        IValidator<AddTaskCommentRequest> validator,
        TimeProvider timeProvider)
    {
        _db = db;
        _access = access;
        _permissions = permissions;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<TaskCommentDto>> ListAsync(Guid taskId, CancellationToken cancellationToken)
    {
        await EnsureTaskVisibleAsync(taskId, cancellationToken);

        var userId = _access.CurrentUserId;
        var canDeleteAny = await _permissions.HasAsync(PermissionCodes.TaskDelete, cancellationToken);

        return await _db.TaskComments
            .AsNoTracking()
            .Where(c => c.TaskItemId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new TaskCommentDto(
                c.Id,
                new UserReferenceDto(c.Author.Id, c.Author.FirstName, c.Author.LastName, c.Author.Email),
                c.Body,
                c.CreatedAt,
                canDeleteAny || c.AuthorId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskCommentDto> AddAsync(Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureTaskVisibleAsync(taskId, cancellationToken);

        var comment = new TaskComment
        {
            TaskItemId = taskId,
            AuthorId = _access.CurrentUserId,
            Body = request.Body.Trim()
        };

        _db.TaskComments.Add(comment);

        await _db.SaveChangesAsync(cancellationToken);

        var comments = await ListAsync(taskId, cancellationToken);

        return comments.Single(c => c.Id == comment.Id);
    }

    public async Task DeleteAsync(Guid taskId, Guid commentId, CancellationToken cancellationToken)
    {
        await EnsureTaskVisibleAsync(taskId, cancellationToken);

        var comment = await _db.TaskComments
            .SingleOrDefaultAsync(c => c.Id == commentId && c.TaskItemId == taskId, cancellationToken)
            ?? throw new NotFoundException("The comment was not found.");

        var isAuthor = comment.AuthorId == _access.CurrentUserId;

        if (!isAuthor && !await _permissions.HasAsync(PermissionCodes.TaskDelete, cancellationToken))
        {
            throw new ForbiddenException("Only the author can delete this comment.");
        }

        comment.IsDeleted = true;
        comment.DeletedAt = _timeProvider.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureTaskVisibleAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        var visible = await _db.TaskItems.AnyAsync(
            t => t.Id == taskId && projects.Any(p => p.Id == t.ProjectId),
            cancellationToken);

        if (!visible)
        {
            throw new NotFoundException("The task was not found.");
        }
    }
}
