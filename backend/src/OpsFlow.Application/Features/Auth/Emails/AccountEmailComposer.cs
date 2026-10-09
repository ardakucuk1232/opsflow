using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Models;
using OpsFlow.Application.Common.Options;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Features.Auth.Emails;

public sealed class AccountEmailComposer
{
    public const string VerifyEmailPath = "/verify-email";
    public const string ResetPasswordPath = "/reset-password";
    public const string AcceptInvitationPath = "/accept-invitation";

    private readonly string _baseUrl;

    public AccountEmailComposer(IOptions<FrontendOptions> frontendOptions)
    {
        _baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
    }

    public EmailMessage EmailVerification(User user, string token, TimeSpan validFor)
    {
        var link = BuildLink(VerifyEmailPath, token);
        var hours = ((int)validFor.TotalHours).ToString(CultureInfo.InvariantCulture);

        return Compose(
            user,
            subject: "OpsFlow hesabınızı doğrulayın",
            intro: "OpsFlow hesabınızın e-posta adresini doğrulamak için aşağıdaki bağlantıyı açın.",
            actionLabel: "E-posta adresimi doğrula",
            link,
            footer: $"Bağlantı {hours} saat boyunca geçerlidir. Bu hesabı siz oluşturmadıysanız bu e-postayı dikkate almayın.");
    }

    public EmailMessage PasswordReset(User user, string token, TimeSpan validFor)
    {
        var link = BuildLink(ResetPasswordPath, token);
        var minutes = ((int)validFor.TotalMinutes).ToString(CultureInfo.InvariantCulture);

        return Compose(
            user,
            subject: "OpsFlow şifre sıfırlama",
            intro: "OpsFlow hesabınız için şifre sıfırlama isteği aldık. Yeni şifrenizi belirlemek için aşağıdaki bağlantıyı açın.",
            actionLabel: "Yeni şifre belirle",
            link,
            footer: $"Bağlantı {minutes} dakika boyunca geçerlidir ve yalnızca bir kez kullanılabilir. Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın, şifreniz değişmez.");
    }

    public EmailMessage Invitation(User invitee, string inviterName, string companyName, string token, TimeSpan validFor)
    {
        var link = BuildLink(AcceptInvitationPath, token);
        var days = ((int)validFor.TotalDays).ToString(CultureInfo.InvariantCulture);

        return Compose(
            invitee,
            subject: $"{companyName} sizi OpsFlow'a davet etti",
            intro: $"{inviterName}, sizi OpsFlow üzerinde {companyName} çalışma alanına davet etti. Daveti kabul etmek ve şifrenizi belirlemek için aşağıdaki bağlantıyı açın.",
            actionLabel: "Daveti kabul et",
            link,
            footer: $"Bağlantı {days} gün boyunca geçerlidir. Bu daveti beklemiyorsanız bu e-postayı dikkate almayın.");
    }

    private string BuildLink(string path, string token) =>
        $"{_baseUrl}{path}#token={Uri.EscapeDataString(token)}";

    private static EmailMessage Compose(
        User user,
        string subject,
        string intro,
        string actionLabel,
        string link,
        string footer)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        var textBody = string.Join(
            "\n\n",
            $"Merhaba {user.FirstName},",
            intro,
            link,
            footer);

        var encoder = HtmlEncoder.Default;
        var encodedLink = encoder.Encode(link);

        var htmlBody = $"""
            <!doctype html>
            <html lang="tr">
            <body style="margin:0;padding:24px;background:#f6f7f9;font-family:Arial,Helvetica,sans-serif;color:#101828;">
              <div style="max-width:520px;margin:0 auto;background:#ffffff;border:1px solid #e4e7ec;border-radius:12px;padding:32px;">
                <p style="margin:0 0 16px;font-size:16px;">Merhaba {encoder.Encode(user.FirstName)},</p>
                <p style="margin:0 0 24px;font-size:15px;line-height:1.5;">{encoder.Encode(intro)}</p>
                <p style="margin:0 0 24px;">
                  <a href="{encodedLink}" style="display:inline-block;background:#4338ca;color:#ffffff;text-decoration:none;font-size:15px;font-weight:bold;padding:12px 20px;border-radius:8px;">{encoder.Encode(actionLabel)}</a>
                </p>
                <p style="margin:0 0 8px;font-size:13px;color:#5b6472;">Buton çalışmazsa bu bağlantıyı tarayıcınıza yapıştırın:</p>
                <p style="margin:0 0 24px;font-size:13px;word-break:break-all;"><a href="{encodedLink}" style="color:#4338ca;">{encodedLink}</a></p>
                <p style="margin:0;font-size:13px;line-height:1.5;color:#5b6472;">{encoder.Encode(footer)}</p>
              </div>
            </body>
            </html>
            """;

        return new EmailMessage(user.Email, fullName, subject, textBody, htmlBody);
    }
}
