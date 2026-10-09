using OpsFlow.Application.Common.Pagination;

namespace OpsFlow.Application.Features.Users;

public interface IUserService
{
    Task<PagedResult<UserSummaryDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken);

    Task<UserSummaryDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<UserSummaryDto> InviteAsync(InviteUserRequest request, CancellationToken cancellationToken);

    Task ResendInvitationAsync(Guid id, CancellationToken cancellationToken);

    Task<UserSummaryDto> UpdateRolesAsync(
        Guid id,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken);

    Task DeactivateAsync(Guid id, string? ipAddress, CancellationToken cancellationToken);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken);
}
