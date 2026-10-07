using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Models;

namespace OpsFlow.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var mimeMessage = MimeMessageFactory.Create(message, _options);
        using var client = new SmtpClient();

        client.Timeout = _options.TimeoutSeconds * 1000;

        await client.ConnectAsync(_options.Host, _options.Port, ToSocketOptions(_options.Security), cancellationToken);

        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private static SecureSocketOptions ToSocketOptions(SmtpSecurity security) => security switch
    {
        SmtpSecurity.None => SecureSocketOptions.None,
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.StartTls
    };
}
