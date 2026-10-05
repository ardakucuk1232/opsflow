namespace OpsFlow.IntegrationTests.Fixtures;

public sealed class RateLimitedApiFactory : OpsFlowApiFactory
{
    public const int Limit = 3;
    
    protected override int AuthPermitLimit => Limit;
}