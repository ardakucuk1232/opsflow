using FluentValidation;

namespace OpsFlow.Application.Features.Auth;

public static class PasswordRules
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static IRuleBuilderOptions<T, string> NewPassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .MinimumLength(MinimumLength)
        .MaximumLength(MaximumLength)
        .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
        .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
}
