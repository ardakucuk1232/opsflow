using Microsoft.Extensions.Caching.Memory;

namespace OpsFlow.Infrastructure.Caching;

public sealed class MemoryCacheBackend : ICacheBackend
{
    private readonly IMemoryCache _cache;

    public MemoryCacheBackend(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(_cache.TryGetValue(key, out byte[]? value) ? value : null);

    public Task SetAsync(string key, byte[] value, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        _cache.Set(key, value, timeToLive);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        foreach (var key in keys)
        {
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }
}
