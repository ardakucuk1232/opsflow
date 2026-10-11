using StackExchange.Redis;

namespace OpsFlow.Infrastructure.Caching;

public sealed class RedisCacheBackend : ICacheBackend
{
    public const string KeyPrefix = "opsflow:";

    private readonly IConnectionMultiplexer _redis;

    public RedisCacheBackend(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var value = await _redis.GetDatabase().StringGetAsync(KeyPrefix + key);

        return value.IsNull ? null : (byte[]?)value;
    }

    public Task SetAsync(string key, byte[] value, TimeSpan timeToLive, CancellationToken cancellationToken) =>
        _redis.GetDatabase().StringSetAsync(KeyPrefix + key, value, timeToLive, When.Always, CommandFlags.None);

    public Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken) =>
        _redis.GetDatabase().KeyDeleteAsync(keys.Select(key => (RedisKey)(KeyPrefix + key)).ToArray());
}
