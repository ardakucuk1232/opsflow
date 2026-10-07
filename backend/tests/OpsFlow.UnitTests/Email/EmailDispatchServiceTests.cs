using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Models;
using OpsFlow.Infrastructure.Email;

namespace OpsFlow.UnitTests.Email;

public class EmailDispatchServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task QueuedMessages_AreHandedToTheSenderInOrder()
    {
        var queue = new EmailQueue();
        var sender = new RecordingEmailSender(expectedAttempts: 2);
        using var service = new EmailDispatchService(queue, sender, NullLogger<EmailDispatchService>.Instance);

        await service.StartAsync(CancellationToken.None);

        queue.TryEnqueue(NewMessage("first"));
        queue.TryEnqueue(NewMessage("second"));

        await sender.AllAttempted.WaitAsync(Timeout);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(["first", "second"], sender.Sent.Select(message => message.Subject));
    }

    [Fact]
    public async Task AFailedDelivery_DoesNotStopTheMessagesThatFollow()
    {
        var queue = new EmailQueue();
        var sender = new RecordingEmailSender(expectedAttempts: 2, failingSubject: "first");
        using var service = new EmailDispatchService(queue, sender, NullLogger<EmailDispatchService>.Instance);

        await service.StartAsync(CancellationToken.None);

        queue.TryEnqueue(NewMessage("first"));
        queue.TryEnqueue(NewMessage("second"));

        await sender.AllAttempted.WaitAsync(Timeout);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(["second"], sender.Sent.Select(message => message.Subject));
    }

    private static EmailMessage NewMessage(string subject) =>
        new("arda@abc.com", "Arda", subject, "Text", "<p>Html</p>");

    private sealed class RecordingEmailSender : IEmailSender
    {
        private readonly TaskCompletionSource _allAttempted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly int _expectedAttempts;
        private readonly string? _failingSubject;
        private int _attempts;

        public RecordingEmailSender(int expectedAttempts, string? failingSubject = null)
        {
            _expectedAttempts = expectedAttempts;
            _failingSubject = failingSubject;
        }

        public ConcurrentQueue<EmailMessage> Sent { get; } = new();

        public Task AllAttempted => _allAttempted.Task;

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            try
            {
                if (message.Subject == _failingSubject)
                {
                    throw new InvalidOperationException("The SMTP server is unavailable.");
                }

                Sent.Enqueue(message);

                return Task.CompletedTask;
            }
            finally
            {
                if (Interlocked.Increment(ref _attempts) == _expectedAttempts)
                {
                    _allAttempted.SetResult();
                }
            }
        }
    }
}
