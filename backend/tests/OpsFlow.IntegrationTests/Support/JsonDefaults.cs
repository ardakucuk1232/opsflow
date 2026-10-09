using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpsFlow.IntegrationTests.Support;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
