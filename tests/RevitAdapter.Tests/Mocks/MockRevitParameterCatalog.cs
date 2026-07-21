using BIMCapabilities.Adapters.Revit.Read.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Tests.Mocks;

internal sealed class MockRevitParameterCatalog : IRevitParameterCatalog
{
    private readonly IReadOnlyList<IRevitParameterCatalogEntry> _parameters;

    internal MockRevitParameterCatalog(IReadOnlyList<IRevitParameterCatalogEntry> parameters)
    {
        _parameters = parameters;
        ObjectsInspected = CountObjectsInspected(parameters);
    }

    public int ObjectsInspected { get; }

    public IReadOnlyList<IRevitParameterCatalogEntry> GetParameters() => _parameters;

    private static int CountObjectsInspected(IReadOnlyList<IRevitParameterCatalogEntry> parameters)
    {
        var familyIds = parameters
            .Select(entry => entry.FamilyId)
            .Where(id => id is not null)
            .Distinct(StringComparer.Ordinal)
            .Count();

        var familyTypeIds = parameters
            .Select(entry => entry.FamilyTypeId)
            .Where(id => id is not null)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return familyIds + familyTypeIds;
    }
}
