namespace OpsFlow.Infrastructure.Tenancy;

public sealed class TenantIsolationException : InvalidOperationException
{
    public TenantIsolationException(string message) : base(message)
    {
    }
}