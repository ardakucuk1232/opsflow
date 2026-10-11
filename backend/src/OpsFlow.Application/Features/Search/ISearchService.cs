namespace OpsFlow.Application.Features.Search;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(string? term, CancellationToken cancellationToken);
}
