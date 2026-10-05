namespace OpsFlow.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<AuthUserDto> GetCurrentUserAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken);
}