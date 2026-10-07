using OpsFlow.Application.Common.Options;
using OpsFlow.Infrastructure.Email;

namespace OpsFlow.UnitTests.Email;

public class EmailOptionsTests
{
    [Theory]
    [InlineData("http://localhost:3000", true)]
    [InlineData("https://app.opsflow.example", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("localhost:3000", false)]
    [InlineData("/relative", false)]
    [InlineData("ftp://files.example", false)]
    public void FrontendBaseUrl_MustBeAnAbsoluteHttpUrl(string? baseUrl, bool expected)
    {
        Assert.Equal(expected, FrontendOptions.HasValidBaseUrl(baseUrl));
    }

    [Theory]
    [InlineData("no-reply@opsflow.test", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("not-an-address", false)]
    public void FromAddress_MustBeAValidEmailAddress(string? address, bool expected)
    {
        Assert.Equal(expected, SmtpOptions.HasValidFromAddress(address));
    }
}
