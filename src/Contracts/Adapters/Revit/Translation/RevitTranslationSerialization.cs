using System.Text.Json;
using System.Text.Json.Serialization;

namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

internal static class RevitTranslationSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
