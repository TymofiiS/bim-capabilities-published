using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

/// <summary>
/// Context passed to a capability handler during rule interpretation.
/// </summary>
public sealed record BimRuleCapabilityInterpretationContext
{
    public required BimRule Rule { get; init; }

    public required string EngineId { get; init; }

    public required BimRuleCapabilityReference CapabilityReference { get; init; }

    public required IReadOnlyDictionary<string, string> MergedConfiguration { get; init; }
}
