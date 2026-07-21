using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Contracts.Engines.Family.Discovery;

/// <summary>
/// Contract for Family Engine discovery atoms.
/// </summary>
public interface IFamilyDiscoveryAtom
{
    string AtomId { get; }

    FamilyDiscoveryResult Discover(FamilyDiscoveryRequest request, IFamilyProvider familyProvider);
}
