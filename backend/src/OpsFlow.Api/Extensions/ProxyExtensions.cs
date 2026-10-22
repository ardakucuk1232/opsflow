using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace OpsFlow.Api.Extensions;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; init; }

    public string[] KnownProxies { get; init; } = [];

    public string[] KnownNetworks { get; init; } = [];
}

public static class ProxyExtensions
{
    public static IServiceCollection AddProxySupport(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = Settings(configuration);

        if (!settings.Enabled)
        {
            return services;
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            foreach (var proxy in settings.KnownProxies.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy.Trim()));
            }

            foreach (var network in settings.KnownNetworks.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network.Trim()));
            }
        });

        return services;
    }

    public static WebApplication UseProxySupport(this WebApplication app)
    {
        if (Settings(app.Configuration).Enabled)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }

    private static ForwardedHeadersSettings Settings(IConfiguration configuration) =>
        configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();
}
