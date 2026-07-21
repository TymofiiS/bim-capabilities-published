namespace BIMCapabilities.Contracts.Rules;

/// <summary>
/// References a documented engine capability used by a rule.
/// </summary>
public sealed record BimRuleCapabilityReference
{
    public required string AtomId { get; init; }

    public IReadOnlyDictionary<string, string>? Configuration { get; init; }
}
