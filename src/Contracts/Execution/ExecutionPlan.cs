namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Describes the ordered runtime execution derived from a BIMRule.
/// </summary>
public sealed record ExecutionPlan
{
    public required string PlanId { get; init; }

    public required string RuleId { get; init; }

    public string? RuleVersion { get; init; }

    public string? ContractVersion { get; init; }

    public string? RuleSourcePath { get; init; }

    public required ExecutionMode Mode { get; init; }

    public required IReadOnlyList<ExecutionStep> Steps { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}
