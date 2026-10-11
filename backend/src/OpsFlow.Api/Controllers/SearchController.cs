using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Features.Search;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _search;

    public SearchController(ISearchService search)
    {
        _search = search;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResultDto>> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        return Ok(await _search.SearchAsync(q, cancellationToken));
    }
}
