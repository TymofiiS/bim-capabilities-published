using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Family.Discovery;

namespace BIMCapabilities.Engines.Family.Atoms.Discovery;

/// <summary>
/// Discovers family types through the adapter family provider.
/// </summary>
public sealed class DiscoverFamilyTypesAtom : IFamilyDiscoveryAtom
{
    public const string DiscoveryAtomId = "family.discovery.family-types";

    public string AtomId => DiscoveryAtomId;

    public FamilyDiscoveryResult Discover(FamilyDiscoveryRequest request, IFamilyProvider familyProvider)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(familyProvider);

        var query = new FamilyQuery
        {
            Categories = request.CategoryNames,
            FamilyNames = request.FamilyNames,
            FamilyTypeNames = request.FamilyTypeNames,
            Scope = new FamilyQueryScope
            {
                Kind = FamilyQueryScopeKind.EntireModel
            },
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        };

        var providerResult = familyProvider.Retrieve(query);
        var familyTypes = providerResult.Families
            .SelectMany(family => family.FamilyTypes ?? [])
            .OrderBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
            .ToArray();

        return FamilyDiscoveryAtomSupport.CreateResult(AtomId, request, providerResult, familyTypes);
    }
}
