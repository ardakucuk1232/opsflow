using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpsFlow.Api.RateLimiting;

namespace OpsFlow.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        EnsurePositive(settings.AuthPermitLimit, nameof(settings.AuthPermitLimit));
        EnsurePositive(settings.AuthWindowSeconds, nameof(settings.AuthWindowSeconds));
        EnsurePositive(settings.SessionPermitLimit, nameof(settings.SessionPermitLimit));
        EnsurePositive(settings.SessionWindowSeconds, nameof(settings.SessionWindowSeconds));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            AddPerIpPolicy(options, RateLimitPolicies.Auth, settings.AuthPermitLimit, settings.AuthWindowSeconds);
            AddPerIpPolicy(options, RateLimitPolicies.Session, settings.SessionPermitLimit, settings.SessionWindowSeconds);

            options.OnRejected = async (context, _) =>
            {
                var httpContext = context.HttpContext;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);

                    httpContext.Response.Headers.RetryAfter =
                        seconds.ToString(CultureInfo.InvariantCulture);
                }

                var problemDetailsService = httpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>();

                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "Too many attempts. Please wait before trying again."
                    }
                });
            };
        });

        return services;
    }

    private static void AddPerIpPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        int windowSeconds)
    {
        options.AddPolicy(policyName, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0
            }));
    }

    private static void EnsurePositive(int value, string settingName)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException(
                $"{RateLimitingOptions.SectionName}:{settingName} must be positive.");
        }
    }
}
