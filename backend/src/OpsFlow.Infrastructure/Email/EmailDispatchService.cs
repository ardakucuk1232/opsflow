using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpsFlow.Application.Common.Interfaces;

namespace OpsFlow.Infrastructure.Email;

public sealed class EmailDispatchService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailDispatchService> _logger;

    public EmailDispatchService(
        EmailQueue queue,
        IEmailSender emailSender,
        ILogger<EmailDispatchService> logger)
    {
        _queue = queue;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _emailSender.SendAsync(message, stoppingToken);

                _logger.LogInformation("Sent the email with subject {Subject}.", message.Subject);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send the email with subject {Subject}.", message.Subject);
            }
        }
    }
}
