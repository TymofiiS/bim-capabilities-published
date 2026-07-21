using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Outcome of a runtime execution including diagnostics and evidence.
/// </summary>
public sealed record ExecutionResult
{
    public required ExecutionStatus Status { get; init; }

    public DiagnosticCollection? Diagnostics { get; init; }

    public EvidenceCollection? Evidence { get; init; }

    public ExecutionSummary? Summary { get; init; }

    public ExecutionCorrelation? Correlation { get; init; }
}
