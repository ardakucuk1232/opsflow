using OpsFlow.Application.Common.Pagination;

namespace OpsFlow.Application.Features.Users;

public sealed class UserListQuery : PageRequest
{
    public string? Search { get; init; }

    public UserStatus? Status { get; init; }

    public Guid? RoleId { get; init; }
}
