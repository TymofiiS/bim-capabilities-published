namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Correlation identifiers shared across execution artifacts.
/// </summary>
public sealed record ExecutionCorrelation
{
    public required string CorrelationId { get; init; }

    public string? ParentCorrelationId { get; init; }

    public string? TraceId { get; init; }

    public string? PlanId { get; init; }
}
