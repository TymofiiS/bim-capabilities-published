namespace BIMCapabilities.Contracts.Rules;

/// <summary>
/// Represents the typed content of a .bimrule file.
/// </summary>
public sealed record BimRule
{
    public required BimRuleMetadata Metadata { get; init; }

    public required IReadOnlyList<BimRuleEngine> Engines { get; init; }

    public required BimRuleExecution Execution { get; init; }

    public required BimRuleReport Report { get; init; }

    public IReadOnlyList<BimRuleExternalReference>? ExternalReferences { get; init; }
}
