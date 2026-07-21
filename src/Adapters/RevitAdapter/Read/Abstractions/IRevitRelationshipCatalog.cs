namespace BIMCapabilities.Adapters.Revit.Read.Abstractions;

/// <summary>
/// Testable abstraction for listing relationships available in the active Revit model.
/// </summary>
public interface IRevitRelationshipCatalog
{
    int ObjectsInspected { get; }

    IReadOnlyList<IRevitRelationshipCatalogEntry> GetRelationships();
}
