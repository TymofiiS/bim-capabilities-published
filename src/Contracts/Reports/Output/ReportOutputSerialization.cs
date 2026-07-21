using System.Text.Json;

namespace BIMCapabilities.Contracts.Reports.Output;

internal static class ReportOutputSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
