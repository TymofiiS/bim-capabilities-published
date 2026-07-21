using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Skeleton parameter provider that returns deterministic stub responses.
/// </summary>
public sealed class ParameterProviderSkeleton : IParameterProvider
{
    public ParameterQueryResult Retrieve(ParameterQuery query)
    {
        ArgumentGuard.ThrowIfNull(query);

        return RevitReadStubResponses.CreateParameterQueryResult(query);
    }
}
