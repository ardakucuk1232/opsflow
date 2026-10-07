using MimeKit;

namespace OpsFlow.Infrastructure.Email;

public enum SmtpSecurity
{
    None = 0,
    StartTls = 1,
    SslOnConnect = 2
}

public sealed class SmtpOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public SmtpSecurity Security { get; init; } = SmtpSecurity.StartTls;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = "OpsFlow";

    public int TimeoutSeconds { get; init; } = 10;

    public static bool HasValidFromAddress(string? address) =>
        !string.IsNullOrWhiteSpace(address)
        && MailboxAddress.TryParse(address, out var mailbox)
        && mailbox.Address.Contains('@', StringComparison.Ordinal);
}
