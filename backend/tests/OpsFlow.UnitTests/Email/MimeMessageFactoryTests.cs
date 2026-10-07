using MimeKit;
using OpsFlow.Application.Common.Models;
using OpsFlow.Infrastructure.Email;

namespace OpsFlow.UnitTests.Email;

public class MimeMessageFactoryTests
{
    private static readonly SmtpOptions Options = new()
    {
        FromAddress = "no-reply@opsflow.test",
        FromName = "OpsFlow"
    };

    private static readonly EmailMessage Message = new(
        ToAddress: "arda@abc.com",
        ToName: "Arda Küçük",
        Subject: "OpsFlow şifre sıfırlama",
        TextBody: "Düz metin gövdesi",
        HtmlBody: "<p>HTML gövdesi</p>");

    [Fact]
    public void Create_SetsTheSenderRecipientAndSubject()
    {
        using var mimeMessage = MimeMessageFactory.Create(Message, Options);

        var from = Assert.IsType<MailboxAddress>(Assert.Single(mimeMessage.From));
        var to = Assert.IsType<MailboxAddress>(Assert.Single(mimeMessage.To));

        Assert.Equal("no-reply@opsflow.test", from.Address);
        Assert.Equal("OpsFlow", from.Name);
        Assert.Equal("arda@abc.com", to.Address);
        Assert.Equal("Arda Küçük", to.Name);
        Assert.Equal("OpsFlow şifre sıfırlama", mimeMessage.Subject);
    }

    [Fact]
    public void Create_IncludesBothThePlainTextAndTheHtmlBody()
    {
        using var mimeMessage = MimeMessageFactory.Create(Message, Options);

        Assert.Equal("Düz metin gövdesi", mimeMessage.TextBody);
        Assert.Equal("<p>HTML gövdesi</p>", mimeMessage.HtmlBody);
    }
}
