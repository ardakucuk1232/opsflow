namespace OpsFlow.IntegrationTests.Fixtures;

public sealed class RateLimitedApiFactory : OpsFlowApiFactory
{
    public const int AuthLimit = 3;
    public const int SessionLimit = 5;

    protected override int AuthPermitLimit => AuthLimit;

    protected override int SessionPermitLimit => SessionLimit;
}
