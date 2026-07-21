namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Transient runtime targeting that defines where a rule applies.
/// </summary>
public sealed record ExecutionScope
{
    public required string ScopeType { get; init; }

    public string? TargetDescription { get; init; }

    public IReadOnlyDictionary<string, string>? Criteria { get; init; }
}
