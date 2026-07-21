using BIMCapabilities.Adapters.Revit.Read.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Tests.Mocks;

internal sealed class MockRevitRelationshipCatalog : IRevitRelationshipCatalog
{
    private readonly IReadOnlyList<IRevitRelationshipCatalogEntry> _relationships;

    internal MockRevitRelationshipCatalog(IReadOnlyList<IRevitRelationshipCatalogEntry> relationships)
    {
        _relationships = relationships;
        ObjectsInspected = CountObjectsInspected(relationships);
    }

    public int ObjectsInspected { get; }

    public IReadOnlyList<IRevitRelationshipCatalogEntry> GetRelationships() => _relationships;

    private static int CountObjectsInspected(IReadOnlyList<IRevitRelationshipCatalogEntry> relationships)
    {
        var sourceIds = relationships
            .Select(entry => entry.Handle.SourceId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        var targetIds = relationships
            .Select(entry => entry.Handle.TargetId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return sourceIds + targetIds;
    }
}
