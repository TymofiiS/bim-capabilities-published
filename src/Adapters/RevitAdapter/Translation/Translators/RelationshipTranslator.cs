using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Mapping;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Translators;

/// <summary>
/// Skeleton relationship translator. Maps relationship handles without business logic.
/// </summary>
internal static class RelationshipTranslator
{
    internal static NormalizedRelationship? Translate(IRevitRelationshipHandle? handle)
    {
        if (handle is null)
        {
            return null;
        }

        return new NormalizedRelationship
        {
            Source = NormalizedIdentifierFactory.Create(handle.SourceId, handle.SourceKind, scope: null),
            Target = NormalizedIdentifierFactory.Create(handle.TargetId, handle.TargetKind, scope: null),
            RelationshipType = handle.RelationshipType,
            Metadata = MetadataCopier.Copy(handle.Metadata)
        };
    }

    internal static IReadOnlyList<NormalizedRelationship>? TranslateCollection(IReadOnlyList<IRevitRelationshipHandle>? handles)
    {
        if (handles is null || handles.Count == 0)
        {
            return null;
        }

        return handles
            .Select(Translate)
            .Where(relationship => relationship is not null)
            .Cast<NormalizedRelationship>()
            .OrderBy(relationship => relationship.Source.Id, StringComparer.Ordinal)
            .ThenBy(relationship => relationship.Target.Id, StringComparer.Ordinal)
            .ToList();
    }
}
