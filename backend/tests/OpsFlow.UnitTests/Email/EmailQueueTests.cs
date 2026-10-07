using OpsFlow.Application.Common.Models;
using OpsFlow.Infrastructure.Email;

namespace OpsFlow.UnitTests.Email;

public class EmailQueueTests
{
    private static readonly EmailMessage Message = new("arda@abc.com", "Arda", "Subject", "Text", "<p>Html</p>");

    [Fact]
    public void TryEnqueue_MakesTheMessageAvailableToTheReader()
    {
        var queue = new EmailQueue();

        Assert.True(queue.TryEnqueue(Message));
        Assert.True(queue.Reader.TryRead(out var read));
        Assert.Same(Message, read);
    }

    [Fact]
    public void TryEnqueue_WhenTheQueueIsFull_ReturnsFalseInsteadOfBlocking()
    {
        var queue = new EmailQueue();

        for (var index = 0; index < EmailQueue.Capacity; index++)
        {
            Assert.True(queue.TryEnqueue(Message));
        }

        Assert.False(queue.TryEnqueue(Message));
    }
}
