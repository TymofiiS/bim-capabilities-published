namespace BIMCapabilities.Contracts.Evidence;

/// <summary>
/// Canonical factual record produced by engine execution.
/// </summary>
public sealed record EvidenceRecord
{
    public required string EvidenceId { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public required EvidenceSource Source { get; init; }

    public EvidenceTarget? Target { get; init; }

    public required EvidenceCategory Category { get; init; }

    public required EvidenceSeverity Severity { get; init; }

    public required string Message { get; init; }

    public IReadOnlyDictionary<string, string>? StructuredData { get; init; }

    public IReadOnlyList<EvidenceAttachment>? Attachments { get; init; }
}
