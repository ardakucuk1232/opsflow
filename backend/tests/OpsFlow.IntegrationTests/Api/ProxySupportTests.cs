using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpsFlow.Api.Extensions;

namespace OpsFlow.IntegrationTests.Api;

public class ProxySupportTests
{
    [Fact]
    public void ForwardedHeaders_AreOnlyTrustedFromConfiguredProxies()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:Enabled"] = "true",
            ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5",
            ["ForwardedHeaders:KnownNetworks:0"] = "172.18.0.0/16"
        });

        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.Equal(1, options.ForwardLimit);
        Assert.Contains(IPAddress.Parse("10.0.0.5"), options.KnownProxies);
        Assert.Contains(System.Net.IPNetwork.Parse("172.18.0.0/16"), options.KnownIPNetworks);
    }

    [Fact]
    public void ForwardedHeaders_AreIgnoredUnlessEnabled()
    {
        var options = Resolve(new Dictionary<string, string?> { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" });

        Assert.Equal(ForwardedHeaders.None, options.ForwardedHeaders);
        Assert.DoesNotContain(IPAddress.Parse("10.0.0.5"), options.KnownProxies);
    }

    private static ForwardedHeadersOptions Resolve(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection().AddOptions();

        services.AddProxySupport(configuration);

        return services.BuildServiceProvider().GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }
}
