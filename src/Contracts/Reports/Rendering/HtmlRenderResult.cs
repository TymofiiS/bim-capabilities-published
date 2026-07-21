namespace BIMCapabilities.Contracts.Reports.Rendering;

/// <summary>
/// Result of rendering a report into HTML.
/// </summary>
public sealed record HtmlRenderResult
{
    public required string Html { get; init; }

    public string? FileContent { get; init; }

    public string ContentType { get; init; } = "text/html; charset=utf-8";

    public string? Title { get; init; }
}
