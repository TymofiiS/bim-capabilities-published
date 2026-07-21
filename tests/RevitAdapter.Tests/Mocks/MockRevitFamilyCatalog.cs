using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests.Mocks;

internal sealed class MockRevitFamilyCatalog : IRevitFamilyCatalog
{
    private readonly IReadOnlyList<IRevitFamilyHandle> _families;

    internal MockRevitFamilyCatalog(IReadOnlyList<IRevitFamilyHandle> families)
    {
        _families = families;
    }

    public IReadOnlyList<IRevitFamilyHandle> GetFamilies() => _families;

    public IReadOnlyList<NormalizedPlacedInstance> GetPlacedInstances(
        IEnumerable<IRevitFamilyHandle> familiesInScope) => [];
}
