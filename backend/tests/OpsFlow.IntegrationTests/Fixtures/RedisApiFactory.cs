namespace OpsFlow.IntegrationTests.Fixtures;

public sealed class RedisApiFactory : OpsFlowApiFactory
{
    private readonly RedisFixture _redis = new();

    protected override string? RedisConnectionString => _redis.ConnectionString;

    public override async Task InitializeAsync()
    {
        await _redis.InitializeAsync();
        await base.InitializeAsync();
    }

    protected override Task DisposeDependenciesAsync() => _redis.DisposeAsync();
}

public sealed class UnreachableRedisApiFactory : OpsFlowApiFactory
{
    protected override string? RedisConnectionString => "127.0.0.1:1,connectTimeout=500";
}
