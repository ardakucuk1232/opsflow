namespace OpsFlow.Application.Features.Auth;

public sealed record LoginRequest(
    string Email,
    string Password);