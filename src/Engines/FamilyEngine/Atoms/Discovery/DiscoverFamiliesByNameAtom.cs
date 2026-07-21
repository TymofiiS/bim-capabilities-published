using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Family.Discovery;

namespace BIMCapabilities.Engines.Family.Atoms.Discovery;

/// <summary>
/// Discovers families by name through the adapter family provider.
/// </summary>
public sealed class DiscoverFamiliesByNameAtom : IFamilyDiscoveryAtom
{
    public const string DiscoveryAtomId = "family.discovery.by-name";

    public string AtomId => DiscoveryAtomId;

    public FamilyDiscoveryResult Discover(FamilyDiscoveryRequest request, IFamilyProvider familyProvider)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(familyProvider);

        var query = new FamilyQuery
        {
            FamilyNames = request.FamilyNames,
            Scope = new FamilyQueryScope
            {
                Kind = FamilyQueryScopeKind.EntireModel
            },
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        };

        var providerResult = familyProvider.Retrieve(query);
        return FamilyDiscoveryAtomSupport.CreateResult(AtomId, request, providerResult);
    }
}
