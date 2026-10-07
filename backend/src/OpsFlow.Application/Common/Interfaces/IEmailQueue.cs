using OpsFlow.Application.Common.Models;

namespace OpsFlow.Application.Common.Interfaces;

public interface IEmailQueue
{
    bool TryEnqueue(EmailMessage message);
}
