namespace OpsFlow.Application.Features.Auth;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
