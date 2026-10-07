namespace OpsFlow.Application.Features.Auth;

public interface IAccountService
{
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);

    Task ResendVerificationEmailAsync(CancellationToken cancellationToken);

    Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    Task ResetPasswordAsync(
        ResetPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);
}
