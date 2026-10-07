using OpsFlow.Application.Common.Models;

namespace OpsFlow.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
