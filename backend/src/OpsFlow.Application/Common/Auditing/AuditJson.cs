using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpsFlow.Application.Common.Auditing;

public static class AuditJson
{
    public const string MaskedValue = "***";

    public static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string? Serialize(IReadOnlyDictionary<string, object?> values) =>
        values.Count == 0 ? null : JsonSerializer.Serialize(values, Options);
}
