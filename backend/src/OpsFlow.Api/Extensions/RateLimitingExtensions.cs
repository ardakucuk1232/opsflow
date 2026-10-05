using System.Globalization;
using System.Threading.RateLimiting;
using OpsFlow.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;

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

        if (settings.AuthPermitLimit <= 0 || settings.AuthWindowSeconds <= 0)
        {
            throw new InvalidOperationException("RateLimiting:AuthPermitLimit and RateLimiting:AuthWindowSeconds must be positive.");
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unkown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(settings.AuthWindowSeconds),
                    QueueLimit = 0
                }));

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
}