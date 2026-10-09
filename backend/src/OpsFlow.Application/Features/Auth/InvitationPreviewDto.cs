namespace OpsFlow.Application.Features.Auth;

public sealed record InvitationPreviewDto(
    string Email,
    string FirstName,
    string LastName,
    string CompanyName);
