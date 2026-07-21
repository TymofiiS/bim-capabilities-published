using System.Text.Json;
using System.Text.Json.Serialization;

namespace BIMCapabilities.Contracts.Execution;

internal static class ExecutionSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
