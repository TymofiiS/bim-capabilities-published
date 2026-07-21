namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Contract for retrieving normalized parameters from the Revit Adapter read layer.
/// </summary>
public interface IParameterProvider
{
    ParameterQueryResult Retrieve(ParameterQuery query);
}
