using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Options;
using OpsFlow.Application.Common.Security;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Attachments;

public sealed class AttachmentService : IAttachmentService
{
    private const int FileNameLimit = 255;

    private readonly IOpsFlowDbContext _db;
    private readonly ProjectAccess _access;
    private readonly ICurrentUserPermissions _permissions;
    private readonly IFileStorage _storage;
    private readonly TaskActivity _activity;
    private readonly StorageOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(
        IOpsFlowDbContext db,
        ProjectAccess access,
        ICurrentUserPermissions permissions,
        IFileStorage storage,
        TaskActivity activity,
        IOptions<StorageOptions> options,
        TimeProvider timeProvider,
        ILogger<AttachmentService> logger)
    {
        _db = db;
        _access = access;
        _permissions = permissions;
        _storage = storage;
        _activity = activity;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AttachmentDto>> ListAsync(Guid taskId, CancellationToken cancellationToken)
    {
        await EnsureTaskVisibleAsync(taskId, cancellationToken);

        var userId = _access.CurrentUserId;
        var canDeleteAny = await _permissions.HasAsync(PermissionCodes.TaskDelete, cancellationToken);

        return await _db.Attachments
            .AsNoTracking()
            .Where(a => a.TaskItemId == taskId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                new UserReferenceDto(a.UploadedByUser.Id, a.UploadedByUser.FirstName, a.UploadedByUser.LastName, a.UploadedByUser.Email),
                a.CreatedAt,
                canDeleteAny || a.UploadedByUserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<AttachmentDto> UploadAsync(Guid taskId, UploadAttachmentRequest request, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(request.FileName ?? string.Empty).Trim();

        if (fileName.Length == 0 || request.Length <= 0)
        {
            throw new ValidationException([new ValidationFailure("file", "A non-empty file is required.")]);
        }

        if (fileName.Length > FileNameLimit)
        {
            throw new ValidationException([new ValidationFailure("file", $"The file name cannot be longer than {FileNameLimit} characters.")]);
        }

        var projectId = await EnsureTaskVisibleAsync(taskId, cancellationToken);

        if (request.Length > _options.MaxFileSizeBytes)
        {
            throw new BusinessRuleException(ErrorCodes.Attachments.FileTooLarge, "The file is larger than the allowed limit.");
        }

        if (!FileTypes.TryGetContentType(fileName, out var contentType))
        {
            throw new BusinessRuleException(ErrorCodes.Attachments.FileTypeNotAllowed, "This file type is not allowed.");
        }

        var header = new byte[FileTypes.HeaderLength];
        var read = await request.Content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

        if (!FileTypes.HeaderMatches(fileName, header[..read]))
        {
            throw new BusinessRuleException(ErrorCodes.Attachments.ContentMismatch, "The file content does not match its extension.");
        }

        request.Content.Position = 0;

        var attachment = new Attachment
        {
            TaskItemId = taskId,
            UploadedByUserId = _access.CurrentUserId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = request.Length
        };

        attachment.StoredFileName = attachment.Id.ToString("N");
        attachment.StoragePath = $"{await CompanyIdAsync(cancellationToken):N}/{attachment.StoredFileName}";

        await _storage.SaveAsync(attachment.StoragePath, request.Content, cancellationToken);

        try
        {
            _db.Attachments.Add(attachment);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _storage.Delete(attachment.StoragePath);
            throw;
        }

        await _activity.ChangedAsync(projectId, taskId, cancellationToken);

        var attachments = await ListAsync(taskId, cancellationToken);

        return attachments.Single(a => a.Id == attachment.Id);
    }

    public async Task<AttachmentContent> OpenAsync(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await LoadVisibleAsync(id, cancellationToken);

        try
        {
            return new AttachmentContent(_storage.OpenRead(attachment.StoragePath), attachment.ContentType, attachment.FileName);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("The file is no longer available.");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await LoadVisibleAsync(id, cancellationToken);

        var isUploader = attachment.UploadedByUserId == _access.CurrentUserId;

        if (!isUploader && !await _permissions.HasAsync(PermissionCodes.TaskDelete, cancellationToken))
        {
            throw new ForbiddenException("Only the uploader can delete this file.");
        }

        attachment.IsDeleted = true;
        attachment.DeletedAt = _timeProvider.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            _storage.Delete(attachment.StoragePath);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Could not delete the stored file of attachment {AttachmentId}.", attachment.Id);
        }

        var projectId = await _db.TaskItems
            .Where(t => t.Id == attachment.TaskItemId)
            .Select(t => t.ProjectId)
            .SingleAsync(cancellationToken);

        await _activity.ChangedAsync(projectId, attachment.TaskItemId!.Value, cancellationToken);
    }

    private async Task<Guid> EnsureTaskVisibleAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        var projectId = await _db.TaskItems
            .Where(t => t.Id == taskId && projects.Any(p => p.Id == t.ProjectId))
            .Select(t => (Guid?)t.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);

        return projectId ?? throw new NotFoundException("The task was not found.");
    }

    private async Task<Attachment> LoadVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        return await _db.Attachments
            .Where(a => a.Id == id
                && _db.TaskItems.Any(t => t.Id == a.TaskItemId && projects.Any(p => p.Id == t.ProjectId)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("The attachment was not found.");
    }

    private async Task<Guid> CompanyIdAsync(CancellationToken cancellationToken)
    {
        var userId = _access.CurrentUserId;

        return await _db.Users.Where(u => u.Id == userId).Select(u => u.CompanyId).SingleAsync(cancellationToken);
    }
}
