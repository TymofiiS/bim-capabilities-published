using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Translators;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal static class Revit2026PlacedInstanceCollector
{
    internal static IReadOnlyList<NormalizedPlacedInstance> Collect(
        Document document,
        IEnumerable<IRevitFamilyHandle> familiesInScope)
    {
        ArgumentGuard.ThrowIfNull(document);

        var familyIds = familiesInScope
            .Select(family => family.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (familyIds.Count == 0)
        {
            return [];
        }

        var instances = new List<NormalizedPlacedInstance>();

        foreach (var instance in new FilteredElementCollector(document)
                     .OfClass(typeof(FamilyInstance))
                     .Cast<FamilyInstance>())
        {
            if (!TryTranslateInstance(instance, familyIds, out var normalizedInstance))
            {
                continue;
            }

            instances.Add(normalizedInstance);
        }

        return instances
            .OrderBy(instance => instance.Identity.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryTranslateInstance(
        FamilyInstance instance,
        ISet<string> familyIds,
        out NormalizedPlacedInstance normalizedInstance)
    {
        normalizedInstance = null!;

        if (!instance.IsValidObject)
        {
            return false;
        }

        try
        {
            var symbol = instance.Symbol;
            var family = symbol?.Family;
            if (symbol is null || family is null || !family.IsValidObject)
            {
                return false;
            }

            if (!familyIds.Contains(family.Id.ToString()))
            {
                return false;
            }

            var displayName = string.IsNullOrWhiteSpace(instance.Name)
                ? instance.Id.ToString()
                : instance.Name;

            normalizedInstance = new NormalizedPlacedInstance
            {
                Identity = PlacedInstanceTranslator.CreateIdentity(instance),
                Name = displayName,
                FamilyName = family.Name,
                FamilyTypeName = symbol.Name,
                CategoryName = family.FamilyCategory?.Name,
                Parameters = ParameterTranslator.TranslateCollection(Revit2026ParameterCollector.Collect(instance)),
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["elementId"] = instance.Id.ToString(),
                    ["familyId"] = family.Id.ToString(),
                    ["familyTypeId"] = symbol.Id.ToString()
                }
            };

            return true;
        }
        catch (Autodesk.Revit.Exceptions.InvalidObjectException)
        {
            return false;
        }
    }
}
