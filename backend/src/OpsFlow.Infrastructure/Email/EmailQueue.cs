using System.Threading.Channels;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Models;

namespace OpsFlow.Infrastructure.Email;

public sealed class EmailQueue : IEmailQueue
{
    public const int Capacity = 500;

    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public bool TryEnqueue(EmailMessage message) => _channel.Writer.TryWrite(message);
}
