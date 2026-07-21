namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Contract for retrieving normalized relationships from the Revit Adapter read layer.
/// </summary>
public interface IRelationshipProvider
{
    RelationshipQueryResult Retrieve(RelationshipQuery query);
}
