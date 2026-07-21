using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Engines.Family;

/// <summary>
/// Contract for the Family Engine selection and target-set operations.
/// </summary>
public interface IFamilyEngine
{
    FamilySelectionResult Select(FamilySelectionRequest request);
}
