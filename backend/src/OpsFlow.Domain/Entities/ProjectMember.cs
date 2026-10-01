using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;

public class ProjectMember : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;
    public DateTimeOffset JoinedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}