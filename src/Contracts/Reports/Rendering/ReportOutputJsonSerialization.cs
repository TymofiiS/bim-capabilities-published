using System.Text.Encodings.Web;
using System.Text.Json;

namespace BIMCapabilities.Contracts.Reports.Rendering;

/// <summary>
/// JSON serialization options for deterministic report rendering.
/// </summary>
public static class ReportOutputJsonSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
