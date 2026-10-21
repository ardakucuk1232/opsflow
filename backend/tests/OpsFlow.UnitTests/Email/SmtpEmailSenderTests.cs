using System.Net;
using System.Net.Sockets;
using System.Text;
using MailKit.Security;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Models;
using OpsFlow.Infrastructure.Email;

namespace OpsFlow.UnitTests.Email;

public sealed class SmtpEmailSenderTests : IAsyncDisposable
{
    private readonly FakeSmtpServer _server = new();

    public ValueTask DisposeAsync() => _server.DisposeAsync();

    [Fact]
    public async Task Send_DeliversTheMessageToTheSmtpServer()
    {
        var sender = CreateSender();

        await sender.SendAsync(
            new EmailMessage("arda@abc.com", "Arda Küçük", "OpsFlow davet", "Davet metni", "<p>Davet</p>"),
            CancellationToken.None);

        var received = await _server.Received.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal("<no-reply@opsflow.test>", received.From);
        Assert.Equal(["<arda@abc.com>"], received.Recipients);
        Assert.Contains("Davet metni", received.Data);
    }

    [Fact]
    public async Task Send_AuthenticatesWhenCredentialsAreConfigured()
    {
        var sender = CreateSender(username: "mailer", password: "secret");

        await sender.SendAsync(
            new EmailMessage("arda@abc.com", "Arda", "Konu", "Metin", "<p>Metin</p>"),
            CancellationToken.None);

        var received = await _server.Received.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("\0mailer\0secret")), received.Credentials);
    }

    [Fact]
    public async Task Send_FailsWhenTheServerCannotBeReached()
    {
        var port = _server.Port;
        await _server.DisposeAsync();

        var sender = CreateSender(port: port);

        await Assert.ThrowsAnyAsync<SocketException>(() => sender.SendAsync(
            new EmailMessage("arda@abc.com", "Arda", "Konu", "Metin", "<p>Metin</p>"),
            CancellationToken.None));
    }

    private SmtpEmailSender CreateSender(string username = "", string password = "", int? port = null) =>
        new(Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1",
            Port = port ?? _server.Port,
            Security = SmtpSecurity.None,
            Username = username,
            Password = password,
            FromAddress = "no-reply@opsflow.test",
            TimeoutSeconds = 5
        }));

    private sealed record ReceivedMail(string From, IReadOnlyList<string> Recipients, string Data, string? Credentials);

    private sealed class FakeSmtpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;
        private bool _disposed;

        public FakeSmtpServer()
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _loop = Task.Run(AcceptAsync);
        }

        public int Port { get; }

        public TaskCompletionSource<ReceivedMail> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await _stop.CancelAsync();
            _listener.Stop();

            try
            {
                await _loop;
            }
            catch (Exception exception) when (exception is OperationCanceledException or SocketException or ObjectDisposedException)
            {
            }
        }

        private async Task AcceptAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                await HandleAsync(client);
            }
        }

        private async Task HandleAsync(TcpClient client)
        {
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };

            var from = string.Empty;
            var recipients = new List<string>();
            string? credentials = null;
            var data = new StringBuilder();

            await writer.WriteLineAsync("220 fake ESMTP");

            while (await reader.ReadLineAsync(_stop.Token) is { } line)
            {
                var command = line.ToUpperInvariant();

                if (command.StartsWith("EHLO", StringComparison.Ordinal))
                {
                    await writer.WriteLineAsync("250-fake");
                    await writer.WriteLineAsync("250 AUTH PLAIN");
                }
                else if (command.StartsWith("AUTH PLAIN", StringComparison.Ordinal))
                {
                    credentials = line.Length > 11 ? line[11..] : await reader.ReadLineAsync(_stop.Token);
                    await writer.WriteLineAsync("235 ok");
                }
                else if (command.StartsWith("MAIL FROM:", StringComparison.Ordinal))
                {
                    from = line[10..].Split(' ')[0];
                    await writer.WriteLineAsync("250 ok");
                }
                else if (command.StartsWith("RCPT TO:", StringComparison.Ordinal))
                {
                    recipients.Add(line[8..].Split(' ')[0]);
                    await writer.WriteLineAsync("250 ok");
                }
                else if (command == "DATA")
                {
                    await writer.WriteLineAsync("354 go");

                    while (await reader.ReadLineAsync(_stop.Token) is { } body && body != ".")
                    {
                        data.AppendLine(body);
                    }

                    await writer.WriteLineAsync("250 queued");
                    Received.TrySetResult(new ReceivedMail(from, recipients, data.ToString(), credentials));
                }
                else if (command == "QUIT")
                {
                    await writer.WriteLineAsync("221 bye");
                    return;
                }
                else
                {
                    await writer.WriteLineAsync("250 ok");
                }
            }
        }
    }
}
