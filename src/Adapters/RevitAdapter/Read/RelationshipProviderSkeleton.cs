using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Skeleton relationship provider that returns deterministic stub responses.
/// </summary>
public sealed class RelationshipProviderSkeleton : IRelationshipProvider
{
    public RelationshipQueryResult Retrieve(RelationshipQuery query)
    {
        ArgumentGuard.ThrowIfNull(query);

        return RevitReadStubResponses.CreateRelationshipQueryResult(query);
    }
}
