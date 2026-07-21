using System.Text.Json;
using System.Text.Json.Serialization;

namespace BIMCapabilities.Contracts.Engines.Registration;

internal static class EngineRegistrationSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
