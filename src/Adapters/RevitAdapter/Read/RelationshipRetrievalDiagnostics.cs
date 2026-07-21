namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Deterministic diagnostic codes emitted by the relationship retrieval provider.
/// </summary>
internal static class RelationshipRetrievalDiagnostics
{
    internal const string RelationshipNotFound = "RelationshipQuery.RelationshipNotFound";

    internal const string UnsupportedRelationship = "RelationshipQuery.UnsupportedRelationship";

    internal const string InvalidQuery = "RelationshipQuery.InvalidQuery";

    internal const string EmptyResult = "RelationshipQuery.EmptyResult";

    internal const string UnsupportedFilter = "RelationshipQuery.UnsupportedFilter";
}
