using FluentValidation;

namespace OpsFlow.Application.Features.Auth;

public sealed class InvitationTokenRequestValidator : AbstractValidator<InvitationTokenRequest>
{
    public InvitationTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
    }
}
