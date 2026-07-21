namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// High-level summary of a runtime execution outcome.
/// </summary>
public sealed record ExecutionSummary
{
    public required ExecutionStatus Status { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public int TotalSteps { get; init; }

    public int CompletedSteps { get; init; }

    public int FailedSteps { get; init; }

    public int SkippedSteps { get; init; }

    public string? Message { get; init; }
}
