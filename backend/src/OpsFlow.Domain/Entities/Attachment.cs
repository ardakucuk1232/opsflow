using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entites;

namespace OpsFlow.Domain.Entities;

public class Attachment : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid CompanyId { get; set; } 

    public Guid? TaskItemId { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
    public TaskItem? TaskItem { get; set; }
    public User UploadedByUser { get; set; } = null!;
}