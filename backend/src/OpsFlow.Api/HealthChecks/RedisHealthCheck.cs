using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpsFlow.Infrastructure.Caching;
using StackExchange.Redis;

namespace OpsFlow.Api.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _redis.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis is not reachable; the cache falls back to the database.");
        }
    }
}

public static class RedisHealthCheckExtensions
{
    public static IHealthChecksBuilder AddRedisCheck(this IHealthChecksBuilder builder, IConfiguration configuration) =>
        CachingExtensions.IsRedisConfigured(configuration)
            ? builder.AddCheck<RedisHealthCheck>("redis", HealthStatus.Degraded)
            : builder;
}
