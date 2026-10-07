using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Features.Auth.Emails;
using OpsFlow.Application.Features.Auth.Tokens;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Exceptions;

namespace OpsFlow.Application.Features.Auth;

public sealed class AccountService : IAccountService
{
    private const string InvalidTokenMessage = "The link is invalid or has expired.";

    private readonly IOpsFlowDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly UserTokenManager _tokens;
    private readonly AccountMailer _mailer;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<VerifyEmailRequest> _verifyEmailValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IOpsFlowDbContext db,
        ITenantContext tenantContext,
        UserTokenManager tokens,
        AccountMailer mailer,
        IPasswordHasher passwordHasher,
        IValidator<VerifyEmailRequest> verifyEmailValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        TimeProvider timeProvider,
        ILogger<AccountService> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _tokens = tokens;
        _mailer = mailer;
        _passwordHasher = passwordHasher;
        _verifyEmailValidator = verifyEmailValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await _verifyEmailValidator.ValidateAndThrowAsync(request, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var token = await _tokens.ConsumeAsync(
            request.Token, UserTokenType.EmailVerification, now, cancellationToken);

        if (token is null)
        {
            throw new BusinessRuleException(ErrorCodes.Auth.InvalidToken, InvalidTokenMessage);
        }

        await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == token.UserId && !u.IsEmailVerified)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.IsEmailVerified, true)
                    .SetProperty(u => u.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);
    }

    public async Task ResendVerificationEmailAsync(CancellationToken cancellationToken)
    {
        if (_tenantContext.UserId is not Guid userId)
        {
            throw new UnauthorizedException("Authentication is required.");
        }

        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("The user associated with this token is no longer available.");
        }

        if (user.IsEmailVerified)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();

        var recentlySent = await _tokens.WasIssuedSinceAsync(
            user.Id, UserTokenType.EmailVerification, now - _mailer.Cooldown, cancellationToken);

        if (recentlySent)
        {
            return;
        }

        await _tokens.InvalidateAsync(user.Id, UserTokenType.EmailVerification, now, cancellationToken);

        var email = _mailer.PrepareEmailVerification(user, now);

        await _db.SaveChangesAsync(cancellationToken);

        _mailer.Send(email);
    }

    public async Task RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _forgotPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(u => u.Company)
            .SingleOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (user is null || !user.IsActive || !user.Company.IsActive || user.Company.IsDeleted)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();

        var recentlySent = await _tokens.WasIssuedSinceAsync(
            user.Id, UserTokenType.PasswordReset, now - _mailer.Cooldown, cancellationToken);

        if (recentlySent)
        {
            return;
        }

        await _tokens.InvalidateAsync(user.Id, UserTokenType.PasswordReset, now, cancellationToken);

        var message = _mailer.PreparePasswordReset(user, now);

        await _db.SaveChangesAsync(cancellationToken);

        _mailer.Send(message);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await _resetPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var token = await _tokens.ConsumeAsync(
            request.Token, UserTokenType.PasswordReset, now, cancellationToken);

        if (token is null)
        {
            throw new BusinessRuleException(ErrorCodes.Auth.InvalidToken, InvalidTokenMessage);
        }

        var passwordHash = _passwordHasher.Hash(request.NewPassword);

        var updatedUsers = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == token.UserId && !u.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.PasswordHash, passwordHash)
                    .SetProperty(u => u.IsEmailVerified, true)
                    .SetProperty(u => u.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);

        if (updatedUsers == 0)
        {
            throw new BusinessRuleException(ErrorCodes.Auth.InvalidToken, InvalidTokenMessage);
        }

        var revokedSessions = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == token.UserId && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, (DateTimeOffset?)now)
                    .SetProperty(t => t.RevokedByIp, ipAddress)
                    .SetProperty(t => t.UpdatedAt, (DateTimeOffset?)now),
                cancellationToken);

        _logger.LogInformation(
            "Password reset completed for user {UserId} from {IpAddress}. Revoked {RevokedCount} active session(s).",
            token.UserId, ipAddress, revokedSessions);
    }
}
