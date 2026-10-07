using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Options;
using OpsFlow.Application.Features.Auth.Emails;
using OpsFlow.Domain.Entities;

namespace OpsFlow.UnitTests.Email;

public class AccountEmailComposerTests
{
    private static readonly User Recipient = new()
    {
        Email = "arda@abc.com",
        FirstName = "Arda",
        LastName = "Küçük"
    };

    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("http://localhost:3000/")]
    public void EmailVerification_LinksToTheVerifyPageWithTheTokenInTheFragment(string baseUrl)
    {
        var composer = CreateComposer(baseUrl);

        var email = composer.EmailVerification(Recipient, "abc-123_XYZ", TimeSpan.FromHours(24));

        Assert.Contains("http://localhost:3000/verify-email#token=abc-123_XYZ", email.TextBody);
        Assert.Contains("http://localhost:3000/verify-email#token=abc-123_XYZ", email.HtmlBody);
        Assert.DoesNotContain("?token=", email.TextBody);
    }

    [Fact]
    public void EmailVerification_IsAddressedToTheUser()
    {
        var email = CreateComposer().EmailVerification(Recipient, "token", TimeSpan.FromHours(24));

        Assert.Equal("arda@abc.com", email.ToAddress);
        Assert.Equal("Arda Küçük", email.ToName);
        Assert.Contains("Merhaba Arda,", email.TextBody);
        Assert.Contains("24 saat", email.TextBody);
    }

    [Fact]
    public void PasswordReset_LinksToTheResetPageAndStatesHowLongTheLinkIsValid()
    {
        var email = CreateComposer().PasswordReset(Recipient, "reset-token", TimeSpan.FromMinutes(60));

        Assert.Contains("http://localhost:3000/reset-password#token=reset-token", email.TextBody);
        Assert.Contains("60 dakika", email.TextBody);
        Assert.Contains("60 dakika", email.HtmlBody);
    }

    [Fact]
    public void HtmlBody_EncodesValuesThatComeFromTheUser()
    {
        var user = new User
        {
            Email = "attacker@abc.com",
            FirstName = "<script>alert(1)</script>",
            LastName = "User"
        };

        var email = CreateComposer().EmailVerification(user, "token", TimeSpan.FromHours(24));

        Assert.DoesNotContain("<script>", email.HtmlBody);
        Assert.Contains("&lt;script&gt;", email.HtmlBody);
    }

    [Fact]
    public void Link_EscapesCharactersThatAreNotSafeInAUrl()
    {
        var email = CreateComposer().PasswordReset(Recipient, "a b&c", TimeSpan.FromMinutes(60));

        Assert.Contains("#token=a%20b%26c", email.TextBody);
    }

    private static AccountEmailComposer CreateComposer(string baseUrl = "http://localhost:3000") =>
        new(Options.Create(new FrontendOptions { BaseUrl = baseUrl }));
}
