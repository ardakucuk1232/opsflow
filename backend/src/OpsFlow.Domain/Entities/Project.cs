using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entites;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;

public class Project : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Guid CreatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<TaskItem> Tasks { get; set; } = [];
}