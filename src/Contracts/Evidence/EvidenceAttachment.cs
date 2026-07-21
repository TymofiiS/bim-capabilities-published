namespace BIMCapabilities.Contracts.Evidence;

/// <summary>
/// Supplementary material attached to an evidence record.
/// </summary>
public sealed record EvidenceAttachment
{
    public required string AttachmentId { get; init; }

    public required string ContentType { get; init; }

    public string? FileName { get; init; }

    public string? Content { get; init; }

    public string? Uri { get; init; }
}
