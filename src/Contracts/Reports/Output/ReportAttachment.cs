namespace BIMCapabilities.Contracts.Reports.Output;

/// <summary>
/// Supplementary material attached to report content.
/// </summary>
public sealed record ReportAttachment
{
    public required string AttachmentId { get; init; }

    public required string ContentType { get; init; }

    public string? FileName { get; init; }

    public string? Content { get; init; }

    public string? Uri { get; init; }
}
