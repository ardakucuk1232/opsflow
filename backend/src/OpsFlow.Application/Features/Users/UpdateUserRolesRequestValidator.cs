using FluentValidation;

namespace OpsFlow.Application.Features.Users;

public sealed class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        RuleFor(x => x.RoleIds).NotEmpty();
    }
}
