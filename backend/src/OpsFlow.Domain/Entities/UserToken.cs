using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entites;


public class UserToken : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }

    public UserTokenType Type { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public Company Company { get; set; } = null!;
    public User User { get; set; } = null!;
}