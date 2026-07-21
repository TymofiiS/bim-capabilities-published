using BIMCapabilities.Contracts.Adapters.Revit.Read;
using TargetSetContracts = BIMCapabilities.Contracts.Engines.Family.TargetSet;

namespace BIMCapabilities.Engines.Family.Generation;

/// <summary>
/// Generates deterministic Family Engine target sets by orchestrating discovery, selection, filtering, and compliance atoms.
/// </summary>
public sealed class FamilyTargetSetGenerator : TargetSetContracts.IFamilyTargetSetGenerator
{
    public const string TargetSetGeneratorId = "family.target-set.generator";

    public string GeneratorId => TargetSetGeneratorId;

    public TargetSetContracts.FamilyTargetSetResult Generate(
        TargetSetContracts.FamilyTargetSetRequest request,
        IFamilyProvider familyProvider)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Definition);
        ArgumentGuard.ThrowIfNull(familyProvider);

        return FamilyTargetSetGeneratorSupport.Generate(GeneratorId, request, familyProvider);
    }
}
