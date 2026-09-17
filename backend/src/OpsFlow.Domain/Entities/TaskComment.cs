using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entites;

namespace OpsFlow.Domain.Entities;

public class TaskComment : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid CompanyId { get; set; }
    public Guid TaskItemId { get; set; }
    public Guid AuthorId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset? EditedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
    public TaskItem TaskItem { get; set; } = null!;
    public User Author { get; set; } = null!;
}