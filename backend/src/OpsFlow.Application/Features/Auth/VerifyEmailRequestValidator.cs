using FluentValidation;

namespace OpsFlow.Application.Features.Auth;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
    }
}
