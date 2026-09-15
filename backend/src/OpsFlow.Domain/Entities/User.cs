using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entites;

namespace OpsFlow.Domain.Entities;

public class User : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid CompanyId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
}