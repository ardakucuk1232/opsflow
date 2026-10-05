namespace OpsFlow.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? CompanyId { get; }

    Guid? UserId { get; }

    bool HasTenant { get; }
}