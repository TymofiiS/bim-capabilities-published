using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Read.Abstractions;

/// <summary>
/// A relationship exposed by the Revit model together with query classification.
/// </summary>
public interface IRevitRelationshipCatalogEntry
{
    IRevitRelationshipHandle Handle { get; }

    RelationshipType QueryRelationshipType { get; }
}
