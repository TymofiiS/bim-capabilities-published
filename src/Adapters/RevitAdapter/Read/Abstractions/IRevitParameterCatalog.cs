namespace BIMCapabilities.Adapters.Revit.Read.Abstractions;

/// <summary>
/// Testable abstraction for listing parameters available in the active Revit model.
/// </summary>
public interface IRevitParameterCatalog
{
    int ObjectsInspected { get; }

    IReadOnlyList<IRevitParameterCatalogEntry> GetParameters();
}
