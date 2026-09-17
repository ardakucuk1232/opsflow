using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entites;

public class TaskItem : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Backlog;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public Guid? AssigneeId { get; set; }

    public Guid ReporterId { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public int BoardOrder { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Project Project { get; set; } = null!;

    public User? Assignee { get; set; }

    public User Reporter { get; set; } = null!;

    public ICollection<TaskComment> Comments { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
}