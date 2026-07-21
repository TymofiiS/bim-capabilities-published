namespace BIMCapabilities.Adapters.Revit.Translation.Abstractions;

/// <summary>
/// Testable abstraction over a Revit family for translation.
/// </summary>
public interface IRevitFamilyHandle
{
    string Id { get; }

    string Name { get; }

    IRevitCategoryHandle? Category { get; }

    IReadOnlyDictionary<string, string>? Metadata { get; }

    IReadOnlyList<IRevitFamilyTypeHandle> FamilyTypes { get; }

    IReadOnlyList<IRevitParameterHandle> Parameters { get; }

    IReadOnlyList<IRevitRelationshipHandle> Relationships { get; }
}
