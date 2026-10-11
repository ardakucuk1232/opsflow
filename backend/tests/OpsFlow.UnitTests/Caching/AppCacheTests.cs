using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpsFlow.Infrastructure.Caching;

namespace OpsFlow.UnitTests.Caching;

public class AppCacheTests
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task GetOrCreate_CallsTheFactoryOnlyOnce()
    {
        var cache = CreateCache(new MemoryCacheBackend(new MemoryCache(new MemoryCacheOptions())));
        var calls = 0;

        Task<List<string>> Load(CancellationToken _)
        {
            calls++;
            return Task.FromResult(new List<string> { "task.create", "task.update" });
        }

        var first = await cache.GetOrCreateAsync("key", Load, Lifetime, CancellationToken.None);
        var second = await cache.GetOrCreateAsync("key", Load, Lifetime, CancellationToken.None);

        Assert.Equal(1, calls);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Remove_MakesTheNextReadLoadAgain()
    {
        var cache = CreateCache(new MemoryCacheBackend(new MemoryCache(new MemoryCacheOptions())));
        var version = 0;

        Task<int> Load(CancellationToken _) => Task.FromResult(++version);

        await cache.GetOrCreateAsync("key", Load, Lifetime, CancellationToken.None);
        await cache.RemoveAsync(["key"], CancellationToken.None);
        var reloaded = await cache.GetOrCreateAsync("key", Load, Lifetime, CancellationToken.None);

        Assert.Equal(2, reloaded);
    }

    [Fact]
    public async Task ABrokenBackend_FallsBackToTheFactory_AndIsSkippedForAWhile()
    {
        var backend = new BrokenBackend();
        var cache = CreateCache(backend);

        var first = await cache.GetOrCreateAsync("key", _ => Task.FromResult(41), Lifetime, CancellationToken.None);
        var second = await cache.GetOrCreateAsync("key", _ => Task.FromResult(42), Lifetime, CancellationToken.None);
        await cache.RemoveAsync(["key"], CancellationToken.None);

        Assert.Equal(41, first);
        Assert.Equal(42, second);
        Assert.Equal(1, backend.Calls);

        _time.Advance(AppCache.BreakDuration);

        await cache.GetOrCreateAsync("key", _ => Task.FromResult(43), Lifetime, CancellationToken.None);

        Assert.Equal(2, backend.Calls);
    }

    [Fact]
    public async Task AnUnreadableEntry_IsTreatedAsAMiss()
    {
        var backend = new MemoryCacheBackend(new MemoryCache(new MemoryCacheOptions()));
        await backend.SetAsync("key", Encoding.UTF8.GetBytes("{not json"), Lifetime, CancellationToken.None);
        var cache = CreateCache(backend);

        var value = await cache.GetOrCreateAsync("key", _ => Task.FromResult(new[] { "fresh" }), Lifetime, CancellationToken.None);

        Assert.Equal(["fresh"], value);
    }

    [Fact]
    public async Task Cancellation_IsNotSwallowed()
    {
        var cache = CreateCache(new CancellingBackend());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cache.GetOrCreateAsync("key", _ => Task.FromResult(1), Lifetime, CancellationToken.None));
    }

    private AppCache CreateCache(ICacheBackend backend) => new(backend, _time, NullLogger<AppCache>.Instance);

    private sealed class BrokenBackend : ICacheBackend
    {
        public int Calls { get; private set; }

        public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("Redis is down.");
        }

        public Task SetAsync(string key, byte[] value, TimeSpan timeToLive, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("Redis is down.");
        }

        public Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("Redis is down.");
        }
    }

    private sealed class CancellingBackend : ICacheBackend
    {
        public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken) =>
            throw new OperationCanceledException();

        public Task SetAsync(string key, byte[] value, TimeSpan timeToLive, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
