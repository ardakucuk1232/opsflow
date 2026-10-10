using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.IntegrationTests.Support;

namespace OpsFlow.IntegrationTests.Fixtures;

public class OpsFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AllowedOrigin = "http://localhost:3000";
    public const string FrontendBaseUrl = "http://localhost:3000";
    public const int MaxFileSizeBytes = 64 * 1024;

    private readonly PostgresFixture _postgres = new();

    private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public CapturingEmailQueue Emails { get; } = new();

    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "opsflow-tests", Guid.NewGuid().ToString("N"));

    protected virtual int AuthPermitLimit => 10_000;

    protected virtual int SessionPermitLimit => 10_000;

    public Task InitializeAsync() => _postgres.InitializeAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();

        if (Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public HubConnection CreateHubConnection(string accessToken) => new HubConnectionBuilder()
        .WithUrl(new Uri(Server.BaseAddress, "hubs/notifications"), options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
        })
        .AddJsonProtocol(options => options.PayloadSerializerOptions = JsonDefaults.Options)
        .Build();

    public async Task ExpireUserTokensAsync(Guid userId)
    {
        var expiredAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        await using var db = _postgres.CreateDbContext(tenantId: null);

        await db.UserTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ExpiresAt, expiredAt));
    }

    public async Task BackdateUserTokensAsync(Guid userId, TimeSpan age)
    {
        var createdAt = DateTimeOffset.UtcNow.Subtract(age);

        await using var db = _postgres.CreateDbContext(tenantId: null);

        await db.UserTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.CreatedAt, createdAt));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailQueue>();
            services.AddSingleton<IEmailQueue>(Emails);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureHostConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.ConnectionString,
                ["Jwt:SigningKey"] = _signingKey,
                ["Cors:AllowedOrigins:0"] = AllowedOrigin,
                ["RateLimiting:AuthPermitLimit"] = AuthPermitLimit.ToString(CultureInfo.InvariantCulture),
                ["RateLimiting:AuthWindowSeconds"] = "60",
                ["RateLimiting:SessionPermitLimit"] = SessionPermitLimit.ToString(CultureInfo.InvariantCulture),
                ["RateLimiting:SessionWindowSeconds"] = "60",
                ["Email:Host"] = "localhost",
                ["Email:Port"] = "2525",
                ["Email:Security"] = "None",
                ["Email:FromAddress"] = "no-reply@opsflow.test",
                ["Frontend:BaseUrl"] = FrontendBaseUrl,
                ["Storage:RootPath"] = StorageRoot,
                ["Storage:MaxFileSizeBytes"] = MaxFileSizeBytes.ToString(CultureInfo.InvariantCulture)
            }));

        return base.CreateHost(builder);
    }
}