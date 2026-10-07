namespace OpsFlow.Api.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; init; } = 10;

    public int AuthWindowSeconds { get; init; } = 60;

    public int SessionPermitLimit { get; init; } = 60;

    public int SessionWindowSeconds { get; init; } = 60;
}
