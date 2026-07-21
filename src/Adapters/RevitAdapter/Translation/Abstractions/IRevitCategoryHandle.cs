namespace BIMCapabilities.Adapters.Revit.Translation.Abstractions;

/// <summary>
/// Testable abstraction over a Revit category for translation.
/// </summary>
public interface IRevitCategoryHandle
{
    string Id { get; }

    string Name { get; }

    IReadOnlyDictionary<string, string>? Metadata { get; }
}
