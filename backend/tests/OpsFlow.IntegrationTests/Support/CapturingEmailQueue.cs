using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Models;

namespace OpsFlow.IntegrationTests.Support;

public sealed partial class CapturingEmailQueue : IEmailQueue
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public bool TryEnqueue(EmailMessage message)
    {
        _messages.Enqueue(message);

        return true;
    }

    public IReadOnlyList<EmailMessage> SentTo(string address) => _messages
        .Where(message => string.Equals(message.ToAddress, address, StringComparison.OrdinalIgnoreCase))
        .ToList();

    public static string ExtractToken(EmailMessage message)
    {
        var match = TokenPattern().Match(message.TextBody);

        Assert.True(match.Success, "The email does not contain a token link.");

        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    [GeneratedRegex(@"#token=([A-Za-z0-9_\-%]+)")]
    private static partial Regex TokenPattern();
}
