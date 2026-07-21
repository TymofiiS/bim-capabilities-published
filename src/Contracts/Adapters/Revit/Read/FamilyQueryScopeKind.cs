namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Scope classification for a family retrieval query.
/// </summary>
public enum FamilyQueryScopeKind
{
    EntireModel,
    SelectedElements,
    SelectedFamilies,
    Custom
}
