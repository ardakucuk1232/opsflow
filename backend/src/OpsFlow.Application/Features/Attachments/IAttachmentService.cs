namespace OpsFlow.Application.Features.Attachments;

public interface IAttachmentService
{
    Task<IReadOnlyList<AttachmentDto>> ListAsync(Guid taskId, CancellationToken cancellationToken);

    Task<AttachmentDto> UploadAsync(Guid taskId, UploadAttachmentRequest request, CancellationToken cancellationToken);

    Task<AttachmentContent> OpenAsync(Guid id, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
