using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read.Revit2026;

/// <summary>
/// Lists MVP relationships from a Revit 2026 document.
/// </summary>
public sealed class Revit2026RelationshipCatalog : IRevitRelationshipCatalog
{
    private readonly Document _document;
    private int _objectsInspected;

    public Revit2026RelationshipCatalog(Document document)
    {
        ArgumentGuard.ThrowIfNull(document);
        _document = document;
    }

    public int ObjectsInspected => _objectsInspected;

    public IReadOnlyList<IRevitRelationshipCatalogEntry> GetRelationships()
    {
        var families = new Revit2026FamilyCatalog(_document).GetFamilies();
        var entries = new List<IRevitRelationshipCatalogEntry>();
        _objectsInspected = 0;

        foreach (var family in families.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            _objectsInspected++;

            foreach (var familyType in family.FamilyTypes.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
            {
                _objectsInspected++;

                entries.Add(RelationshipRetrievalSupport.CreateEntry(
                    family.Id,
                    "family",
                    familyType.Id,
                    "familyType",
                    NormalizedRelationshipType.TypeDefinition,
                    RelationshipType.FamilyType,
                    "familyType"));
            }

            foreach (var relationship in family.Relationships.OrderBy(
                         candidate => candidate.SourceId,
                         StringComparer.Ordinal))
            {
                entries.Add(MapFamilyRelationship(relationship));
            }
        }

        return entries
            .OrderBy(entry => entry.Handle.SourceId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Handle.TargetId, StringComparer.Ordinal)
            .ToList();
    }

    private static IRevitRelationshipCatalogEntry MapFamilyRelationship(IRevitRelationshipHandle relationship)
    {
        if (relationship.Metadata is null ||
            !relationship.Metadata.TryGetValue("queryRelationshipType", out var queryRelationshipTypeText) ||
            !Enum.TryParse(queryRelationshipTypeText, out RelationshipType queryRelationshipType))
        {
            queryRelationshipType = RelationshipType.Reference;
        }

        return new RevitRelationshipCatalogEntry
        {
            Handle = relationship,
            QueryRelationshipType = queryRelationshipType
        };
    }
}
