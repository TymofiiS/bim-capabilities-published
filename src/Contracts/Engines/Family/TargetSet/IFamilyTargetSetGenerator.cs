using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Contracts.Engines.Family.TargetSet;

/// <summary>
/// Contract for the Family Engine target set generator.
/// </summary>
public interface IFamilyTargetSetGenerator
{
    string GeneratorId { get; }

    FamilyTargetSetResult Generate(FamilyTargetSetRequest request, IFamilyProvider familyProvider);
}
