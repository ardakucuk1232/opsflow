using Microsoft.Net.Http.Headers;
using OpsFlow.Api.Cors;

namespace OpsFlow.Api.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(CorsSettings.SectionName)
            .Get<CorsSettings>() ?? new CorsSettings();

        var origins = settings.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .ToArray();

        if (origins.Contains("*"))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must list explicit origins. The wildcard '*' is not allowed.");
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(origins)
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                    .WithHeaders(
                        HeaderNames.Authorization,
                        HeaderNames.ContentType,
                        HeaderNames.XRequestedWith,
                        "X-SignalR-User-Agent")
                    .WithExposedHeaders(HeaderNames.RetryAfter)
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }
}