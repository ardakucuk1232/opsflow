using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entites;

public class RefreshToken : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }

    public Company Company { get; set; } = null!;
    public User User { get; set; } = null!;

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}