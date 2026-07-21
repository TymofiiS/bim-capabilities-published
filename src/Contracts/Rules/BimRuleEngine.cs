namespace BIMCapabilities.Contracts.Rules;

/// <summary>
/// Defines one engine participation entry in rule composition.
/// </summary>
public sealed record BimRuleEngine
{
    public required string EngineId { get; init; }

    public required int Order { get; init; }

    public IReadOnlyDictionary<string, string>? Configuration { get; init; }

    public IReadOnlyList<BimRuleCapabilityReference>? Capabilities { get; init; }
}
