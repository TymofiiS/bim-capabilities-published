namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Contract for retrieving normalized families from the Revit Adapter read layer.
/// </summary>
public interface IFamilyProvider
{
    FamilyQueryResult Retrieve(FamilyQuery query);
}
