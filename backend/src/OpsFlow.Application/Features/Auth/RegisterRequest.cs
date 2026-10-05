namespace OpsFlow.Application.Features.Auth;

public sealed record RegisterRequest(
    string CompanyName,
    string FirstName,
    string LastName,
    string Email,
    string Password);