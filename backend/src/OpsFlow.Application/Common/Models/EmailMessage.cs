namespace OpsFlow.Application.Common.Models;

public sealed record EmailMessage(
    string ToAddress,
    string ToName,
    string Subject,
    string TextBody,
    string HtmlBody);
