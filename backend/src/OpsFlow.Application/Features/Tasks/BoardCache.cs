using OpsFlow.Application.Common.Caching;
using OpsFlow.Application.Common.Interfaces;

namespace OpsFlow.Application.Features.Tasks;

public sealed class BoardCache
{
    private readonly IAppCache _cache;
    private readonly ITenantContext _tenantContext;

    public BoardCache(IAppCache cache, ITenantContext tenantContext)
    {
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public Task<IReadOnlyList<TaskSummaryDto>> GetOrLoadAsync(
        Guid projectId,
        Func<CancellationToken, Task<IReadOnlyList<TaskSummaryDto>>> load,
        CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync(Key(projectId), load, CacheKeys.BoardLifetime, cancellationToken);

    public Task InvalidateAsync(Guid projectId, CancellationToken cancellationToken) =>
        _cache.RemoveAsync([Key(projectId)], cancellationToken);

    private string Key(Guid projectId) =>
        CacheKeys.Board(
            _tenantContext.CompanyId ?? throw new InvalidOperationException("A tenant is required to use the board cache."),
            projectId);
}
