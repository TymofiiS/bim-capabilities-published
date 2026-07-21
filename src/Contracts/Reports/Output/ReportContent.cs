namespace BIMCapabilities.Contracts.Reports.Output;

/// <summary>
/// Renderer-neutral content within a report section.
/// </summary>
public sealed record ReportContent
{
    public string? Text { get; init; }

    public IReadOnlyDictionary<string, string>? StructuredData { get; init; }

    public IReadOnlyList<ReportReference>? EvidenceReferences { get; init; }

    public IReadOnlyList<ReportReference>? DiagnosticReferences { get; init; }

    public IReadOnlyList<ReportAttachment>? Attachments { get; init; }
}
