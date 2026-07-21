using System.Text.Json;
using System.Text.Json.Serialization;

namespace BIMCapabilities.Contracts.Evidence;

internal static class EvidenceSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
