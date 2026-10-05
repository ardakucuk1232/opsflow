using OpsFlow.Application.Common.Interfaces;

namespace OpsFlow.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? CompanyId { get; private set; }

    public Guid? UserId { get; private set; }

    public bool HasTenant => CompanyId.HasValue;

    public void Set(Guid companyId, Guid userId)
    {
        if (HasTenant)
        {
            throw new InvalidOperationException("The tenant context has already been set for this request");
        }

        CompanyId = companyId;
        UserId = userId;
    }
}