using System.Text.Json;
using System.Text.Json.Serialization;

namespace BIMCapabilities.Contracts.Engines.Family;

internal static class FamilyEngineSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
