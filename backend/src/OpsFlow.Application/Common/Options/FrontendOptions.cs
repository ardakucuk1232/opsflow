namespace OpsFlow.Application.Common.Options;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; init; } = string.Empty;

    public static bool HasValidBaseUrl(string? baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
