using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Engines.Family.Atoms.Selection;

/// <summary>
/// Selects discovered families by relationship criteria.
/// </summary>
public sealed class SelectFamiliesByRelationshipAtom : SelectionContracts.IFamilySelectionAtom
{
    public const string SelectionAtomId = "family.selection.by-relationship";

    public string AtomId => SelectionAtomId;

    public SelectionContracts.FamilySelectionResult Select(SelectionContracts.FamilySelectionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria?.Relationships);

        var candidates = request.DiscoveryResult.Families ?? [];
        var selected = FamilySelectionAtomSupport.SelectByRelationship(candidates, request.Criteria.Relationships);
        return FamilySelectionAtomSupport.CreateResult(AtomId, request, selected);
    }
}
