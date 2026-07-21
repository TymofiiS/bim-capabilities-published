namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Ordered step within a runtime execution plan.
/// </summary>
public sealed record ExecutionStep
{
    public required string StepId { get; init; }

    public required string Name { get; init; }

    public required string StepType { get; init; }

    public required int Order { get; init; }

    public IReadOnlyDictionary<string, string>? Configuration { get; init; }
}
