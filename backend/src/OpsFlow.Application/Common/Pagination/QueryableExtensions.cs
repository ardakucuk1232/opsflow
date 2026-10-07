using Microsoft.EntityFrameworkCore;

namespace OpsFlow.Application.Common.Pagination;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResult<T>([], page.Page, page.PageSize, 0);
        }

        var items = await query
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page.Page, page.PageSize, totalCount);
    }
}
