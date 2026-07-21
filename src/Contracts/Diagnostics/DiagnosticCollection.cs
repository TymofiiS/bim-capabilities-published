namespace BIMCapabilities.Contracts.Diagnostics;

/// <summary>
/// Aggregate container for diagnostic records produced during execution.
/// </summary>
public sealed record DiagnosticCollection
{
    public required string CollectionId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyList<DiagnosticRecord> Records { get; init; } = [];
}
