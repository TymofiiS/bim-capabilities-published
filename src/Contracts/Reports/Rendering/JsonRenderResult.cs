namespace BIMCapabilities.Contracts.Reports.Rendering;

/// <summary>
/// Result of rendering a report into JSON.
/// </summary>
public sealed record JsonRenderResult
{
    public required string Json { get; init; }

    public string? DocumentContent { get; init; }

    public string ContentType { get; init; } = "application/json; charset=utf-8";

    public string? Title { get; init; }
}
