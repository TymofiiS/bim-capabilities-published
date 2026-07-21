namespace BIMCapabilities.Adapters.Revit.Translation.Abstractions;

/// <summary>
/// Testable abstraction over a Revit family type for translation.
/// </summary>
public interface IRevitFamilyTypeHandle
{
    string Id { get; }

    string Name { get; }

    IReadOnlyDictionary<string, string>? Metadata { get; }

    IReadOnlyList<IRevitParameterHandle> Parameters { get; }
}
