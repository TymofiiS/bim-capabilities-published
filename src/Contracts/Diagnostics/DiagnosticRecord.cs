namespace BIMCapabilities.Contracts.Diagnostics;

/// <summary>
/// Canonical record describing what happened during execution.
/// </summary>
public sealed record DiagnosticRecord
{
    public required string DiagnosticId { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public required DiagnosticSource Source { get; init; }

    public required DiagnosticCategory Category { get; init; }

    public required DiagnosticSeverity Severity { get; init; }

    public required string Message { get; init; }

    public IReadOnlyDictionary<string, string>? StructuredMetadata { get; init; }

    public DiagnosticContext? Context { get; init; }

    public string? CorrelationId { get; init; }

    public string? ParentCorrelationId { get; init; }

    public string? TraceId { get; init; }
}
