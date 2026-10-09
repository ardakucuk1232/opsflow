namespace OpsFlow.Application.Common.Options;

public sealed class AccountOptions
{
    public const string SectionName = "Account";

    public int EmailVerificationTokenHours { get; init; } = 24;

    public int PasswordResetTokenMinutes { get; init; } = 60;

    public int InvitationTokenDays { get; init; } = 7;

    public int EmailCooldownSeconds { get; init; } = 60;
}
