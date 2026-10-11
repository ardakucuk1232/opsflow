namespace OpsFlow.Infrastructure.Caching;

public interface ICacheBackend
{
    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, byte[] value, TimeSpan timeToLive, CancellationToken cancellationToken);

    Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);
}
