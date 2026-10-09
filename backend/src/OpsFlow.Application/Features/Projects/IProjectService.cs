using OpsFlow.Application.Common.Pagination;

namespace OpsFlow.Application.Features.Projects;

public interface IProjectService
{
    Task<PagedResult<ProjectSummaryDto>> ListAsync(ProjectListQuery query, CancellationToken cancellationToken);

    Task<ProjectDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<ProjectDetailDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);

    Task<ProjectDetailDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ProjectDetailDto> AddMemberAsync(Guid id, AddProjectMemberRequest request, CancellationToken cancellationToken);

    Task<ProjectDetailDto> UpdateMemberAsync(
        Guid id,
        Guid userId,
        UpdateProjectMemberRequest request,
        CancellationToken cancellationToken);

    Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken);
}
