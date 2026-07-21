namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Scope classification for a parameter retrieval query.
/// </summary>
public enum ParameterQueryScopeKind
{
    EntireModel,
    SelectedElements,
    SelectedFamilies,
    SelectedFamilyTypes,
    Custom
}
