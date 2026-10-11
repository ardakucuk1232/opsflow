namespace OpsFlow.Application.Common.Caching;

public interface IAppCache
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan timeToLive,
        CancellationToken cancellationToken);

    Task RemoveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);
}
