using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Skeleton family provider that returns deterministic stub responses.
/// </summary>
public sealed class FamilyProviderSkeleton : IFamilyProvider
{
    public FamilyQueryResult Retrieve(FamilyQuery query)
    {
        ArgumentGuard.ThrowIfNull(query);

        return RevitReadStubResponses.CreateFamilyQueryResult(query);
    }
}
