using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Engines.Family.Atoms.Selection;

/// <summary>
/// Selects discovered families by parameter criteria.
/// </summary>
public sealed class SelectFamiliesByParameterAtom : SelectionContracts.IFamilySelectionAtom
{
    public const string SelectionAtomId = "family.selection.by-parameter";

    public string AtomId => SelectionAtomId;

    public SelectionContracts.FamilySelectionResult Select(SelectionContracts.FamilySelectionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria?.Parameters);

        var candidates = request.DiscoveryResult.Families ?? [];
        var selected = FamilySelectionAtomSupport.SelectByParameter(candidates, request.Criteria.Parameters);
        return FamilySelectionAtomSupport.CreateResult(AtomId, request, selected);
    }
}
