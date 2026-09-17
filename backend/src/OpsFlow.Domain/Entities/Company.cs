using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entites;

public class Company : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Free;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}