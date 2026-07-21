namespace BIMCapabilities.Adapters.Revit.Translation.Abstractions;

/// <summary>
/// Testable abstraction over a Revit element for translation.
/// </summary>
public interface IRevitElementHandle
{
    string Id { get; }

    string Name { get; }

    IRevitCategoryHandle? Category { get; }

    IReadOnlyDictionary<string, string>? Metadata { get; }

    IReadOnlyList<IRevitParameterHandle> Parameters { get; }

    IReadOnlyList<IRevitRelationshipHandle> Relationships { get; }
}
