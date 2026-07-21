using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read.Abstractions;

/// <summary>
/// Testable abstraction for listing families available in the active Revit model.
/// </summary>
public interface IRevitFamilyCatalog
{
    IReadOnlyList<IRevitFamilyHandle> GetFamilies();

    IReadOnlyList<NormalizedPlacedInstance> GetPlacedInstances(IEnumerable<IRevitFamilyHandle> familiesInScope);
}
