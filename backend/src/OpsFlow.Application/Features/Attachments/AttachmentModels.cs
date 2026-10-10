using OpsFlow.Application.Features.Projects;

namespace OpsFlow.Application.Features.Attachments;

public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    UserReferenceDto UploadedBy,
    DateTimeOffset CreatedAt,
    bool CanDelete);

public sealed record UploadAttachmentRequest(string FileName, long Length, Stream Content);

public sealed record AttachmentContent(Stream Content, string ContentType, string FileName);
