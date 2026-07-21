namespace BIMCapabilities.Contracts.Rules;

/// <summary>
/// Defines how a rule should be executed.
/// </summary>
public sealed record BimRuleExecution
{
    public required string TargetPlatform { get; init; }

    public required string ExecutionMode { get; init; }

    public bool ValidationEnabled { get; init; } = true;

    public bool FixEnabled { get; init; }

    public bool DryRun { get; init; }

    public bool RequireUserApprovalBeforeModification { get; init; }

    public string? FailureBehavior { get; init; }
}
