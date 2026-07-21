using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Read;

internal sealed class RevitRelationshipCatalogEntry : IRevitRelationshipCatalogEntry
{
    public required IRevitRelationshipHandle Handle { get; init; }

    public required RelationshipType QueryRelationshipType { get; init; }
}
