using OpsFlow.Application.Common.Pagination;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Projects;

public sealed class ProjectListQuery : PageRequest
{
    public string? Search { get; init; }

    public ProjectStatus? Status { get; init; }

    public bool MemberOnly { get; init; }
}
