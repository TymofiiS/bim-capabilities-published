namespace BIMCapabilities.Adapters.Revit.Translation.Abstractions;

/// <summary>
/// Testable abstraction over a Revit parameter for translation.
/// </summary>
public interface IRevitParameterHandle
{
    string Id { get; }

    string Name { get; }

    string? Value { get; }

    string StorageType { get; }

    bool IsSharedParameter { get; }

    IReadOnlyDictionary<string, string>? Metadata { get; }
}
