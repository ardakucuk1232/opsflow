using MimeKit;
using OpsFlow.Application.Common.Models;

namespace OpsFlow.Infrastructure.Email;

public static class MimeMessageFactory
{
    public static MimeMessage Create(EmailMessage message, SmtpOptions options)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        mimeMessage.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mimeMessage.Subject = message.Subject;

        mimeMessage.Body = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody
        }.ToMessageBody();

        return mimeMessage;
    }
}
