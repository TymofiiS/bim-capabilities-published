using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Abstractions;

/// <summary>
/// Testable abstraction over a Revit relationship for translation.
/// </summary>
public interface IRevitRelationshipHandle
{
    string SourceId { get; }

    string SourceKind { get; }

    string TargetId { get; }

    string TargetKind { get; }

    NormalizedRelationshipType RelationshipType { get; }

    IReadOnlyDictionary<string, string>? Metadata { get; }
}
