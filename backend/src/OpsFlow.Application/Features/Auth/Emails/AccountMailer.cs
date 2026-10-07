using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Models;
using OpsFlow.Application.Common.Options;
using OpsFlow.Application.Features.Auth.Tokens;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Auth.Emails;

public sealed class AccountMailer
{
    private readonly UserTokenManager _tokens;
    private readonly AccountEmailComposer _composer;
    private readonly IEmailQueue _emailQueue;
    private readonly AccountOptions _options;
    private readonly ILogger<AccountMailer> _logger;

    public AccountMailer(
        UserTokenManager tokens,
        AccountEmailComposer composer,
        IEmailQueue emailQueue,
        IOptions<AccountOptions> options,
        ILogger<AccountMailer> logger)
    {
        _tokens = tokens;
        _composer = composer;
        _emailQueue = emailQueue;
        _options = options.Value;
        _logger = logger;
    }

    public TimeSpan Cooldown => TimeSpan.FromSeconds(_options.EmailCooldownSeconds);

    public EmailMessage PrepareEmailVerification(User user, DateTimeOffset now)
    {
        var lifetime = TimeSpan.FromHours(_options.EmailVerificationTokenHours);
        var token = _tokens.Issue(user, UserTokenType.EmailVerification, now, lifetime);

        return _composer.EmailVerification(user, token, lifetime);
    }

    public EmailMessage PreparePasswordReset(User user, DateTimeOffset now)
    {
        var lifetime = TimeSpan.FromMinutes(_options.PasswordResetTokenMinutes);
        var token = _tokens.Issue(user, UserTokenType.PasswordReset, now, lifetime);

        return _composer.PasswordReset(user, token, lifetime);
    }

    public void Send(EmailMessage message)
    {
        if (!_emailQueue.TryEnqueue(message))
        {
            _logger.LogError("The email queue is full. Dropped the email with subject {Subject}.", message.Subject);
        }
    }
}
