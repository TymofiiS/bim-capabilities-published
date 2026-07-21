namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Describes the requested execution behavior for a BIMRule workflow.
/// </summary>
public sealed record ExecutionRequest
{
    public required ExecutionMode Mode { get; init; }

    public bool DryRun { get; init; }

    public bool RequireUserApprovalBeforeModification { get; init; }

    public DateTimeOffset RequestedAt { get; init; }

    public string? RequestedBy { get; init; }
}
