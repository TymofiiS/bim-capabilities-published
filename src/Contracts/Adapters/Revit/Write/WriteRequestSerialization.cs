using System.Text.Json;
using System.Text.Json.Serialization;

namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

internal static class WriteRequestSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
