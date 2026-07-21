using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Engines.Family.Discovery;
/// </summary>
public sealed record FamilyDiscoveryStatistics
{
    public int DiscoveredFamilies { get; init; }

    public int DiscoveredFamilyTypes { get; init; }

    public int ProviderRetrievedFamilies { get; init; }

    public IReadOnlyDictionary<string, int>? CountsByCategory { get; init; }
}

/// <summary>
/// Result of a Family Engine discovery operation.
/// </summary>
public sealed record FamilyDiscoveryResult
{
    public required string AtomId { get; init; }

    public IReadOnlyList<NormalizedFamily>? Families { get; init; }

    public IReadOnlyList<NormalizedFamilyType>? FamilyTypes { get; init; }

    public IReadOnlyList<NormalizedPlacedInstance>? PlacedInstances { get; init; }

    public FamilyDiscoveryStatistics? Statistics { get; init; }

    public IReadOnlyList<FamilyEngineDiagnostic>? Diagnostics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
