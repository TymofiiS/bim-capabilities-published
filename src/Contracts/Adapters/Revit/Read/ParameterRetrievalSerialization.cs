using System.Text.Json;
using System.Text.Json.Serialization;

namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

internal static class ParameterRetrievalSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
