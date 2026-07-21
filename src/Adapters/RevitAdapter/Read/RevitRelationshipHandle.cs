using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

internal sealed class RevitRelationshipHandle : IRevitRelationshipHandle
{
    public RevitRelationshipHandle(
        string sourceId,
        string sourceKind,
        string targetId,
        string targetKind,
        NormalizedRelationshipType relationshipType,
        RelationshipType queryRelationshipType,
        string referenceType)
    {
        SourceId = sourceId;
        SourceKind = sourceKind;
        TargetId = targetId;
        TargetKind = targetKind;
        RelationshipType = relationshipType;
        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["queryRelationshipType"] = queryRelationshipType.ToString(),
            ["referenceType"] = referenceType
        };
    }

    public string SourceId { get; }

    public string SourceKind { get; }

    public string TargetId { get; }

    public string TargetKind { get; }

    public NormalizedRelationshipType RelationshipType { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }
}
