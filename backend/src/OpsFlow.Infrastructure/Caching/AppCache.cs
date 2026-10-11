using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpsFlow.Application.Common.Caching;

namespace OpsFlow.Infrastructure.Caching;

public sealed class AppCache : IAppCache
{
    public static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICacheBackend _backend;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AppCache> _logger;
    private long _unavailableUntil;

    public AppCache(ICacheBackend backend, TimeProvider timeProvider, ILogger<AppCache> logger)
    {
        _backend = backend;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        var cached = await TryAsync(key, () => _backend.GetAsync(key, cancellationToken));

        if (cached is not null && TryDeserialize<T>(key, cached, out var value))
        {
            return value;
        }

        var created = await factory(cancellationToken);

        await TryAsync(key, async () =>
        {
            await _backend.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(created, JsonOptions), timeToLive, cancellationToken);
            return true;
        });

        return created;
    }

    public async Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return;
        }

        await TryAsync(string.Join(", ", keys), async () =>
        {
            await _backend.RemoveAsync(keys, cancellationToken);
            return true;
        });
    }

    private bool IsAvailable => _timeProvider.GetUtcNow().UtcTicks >= Interlocked.Read(ref _unavailableUntil);

    private async Task<TResult?> TryAsync<TResult>(string key, Func<Task<TResult>> operation)
    {
        if (!IsAvailable)
        {
            return default;
        }

        try
        {
            return await operation();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Interlocked.Exchange(ref _unavailableUntil, _timeProvider.GetUtcNow().Add(BreakDuration).UtcTicks);

            _logger.LogWarning(
                exception,
                "The cache is unavailable (key {CacheKey}); serving from the database for the next {Seconds} seconds.",
                key,
                BreakDuration.TotalSeconds);

            return default;
        }
    }

    private bool TryDeserialize<T>(string key, byte[] data, [MaybeNullWhen(false)] out T value)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(data, JsonOptions);
            return value is not null;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Ignoring an unreadable cache entry {CacheKey}.", key);
            value = default;
            return false;
        }
    }
}
