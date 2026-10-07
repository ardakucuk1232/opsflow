using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace OpsFlow.IntegrationTests.Fixtures;

public class OpsFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AllowedOrigin = "http://localhost:3000";
    public const string FrontendBaseUrl = "http://localhost:3000";

    private readonly PostgresFixture _postgres = new();

    private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    protected virtual int AuthPermitLimit => 10_000;

    protected virtual int SessionPermitLimit => 10_000;

    public Task InitializeAsync() => _postgres.InitializeAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

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
                ["Frontend:BaseUrl"] = FrontendBaseUrl
            }));

        return base.CreateHost(builder);
    }
}