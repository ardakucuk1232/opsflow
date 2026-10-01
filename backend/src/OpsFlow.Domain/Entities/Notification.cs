using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;

public class Notification : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }
    public Guid RecipientUserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public Company Company { get; set; } = null!;
    public User RecipientUser { get; set; } = null!;
}