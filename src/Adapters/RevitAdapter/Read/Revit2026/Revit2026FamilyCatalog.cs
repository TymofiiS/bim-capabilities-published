using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Revit2026;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read.Revit2026;

/// <summary>
/// Lists MVP families from a Revit 2026 document.
/// </summary>
public sealed class Revit2026FamilyCatalog : IRevitFamilyCatalog
{
    private readonly Document _document;
    private readonly Action<int, int, string>? _progressReporter;
    private IReadOnlyList<IRevitFamilyHandle>? _cachedFamilies;

    public Revit2026FamilyCatalog(Document document)
        : this(document, progressReporter: null)
    {
    }

    public Revit2026FamilyCatalog(Document document, Action<int, int, string>? progressReporter)
    {
        ArgumentGuard.ThrowIfNull(document);
        _document = document;
        _progressReporter = progressReporter;
    }

    public IReadOnlyList<IRevitFamilyHandle> GetFamilies()
    {
        if (_cachedFamilies is not null)
        {
            return _cachedFamilies;
        }

        var families = new FilteredElementCollector(_document)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .Where(family => IsSupportedCategory(family.FamilyCategory))
            .OrderBy(family => family.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var handles = new List<IRevitFamilyHandle>(families.Length);
        for (var index = 0; index < families.Length; index++)
        {
            var family = families[index];
            var currentStep = index + 1;
            _progressReporter?.Invoke(
                currentStep,
                families.Length,
                $"Reading family {currentStep} of {families.Length}: {family.Name}");

            handles.Add(new Revit2026FamilyHandle(family, _document));
        }

        _cachedFamilies = handles;
        return _cachedFamilies;
    }

    public IReadOnlyList<NormalizedPlacedInstance> GetPlacedInstances(IEnumerable<IRevitFamilyHandle> familiesInScope)
    {
        return Revit2026PlacedInstanceCollector.Collect(_document, familiesInScope);
    }

    private static bool IsSupportedCategory(Category? category)
    {
        return category is not null && SupportedFamilyCategories.Names.Contains(category.Name);
    }
}
