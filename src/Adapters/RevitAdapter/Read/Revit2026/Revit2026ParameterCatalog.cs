using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Revit2026;

namespace BIMCapabilities.Adapters.Revit.Read.Revit2026;

/// <summary>
/// Lists MVP parameters from a Revit 2026 document via family and family type inspection.
/// </summary>
public sealed class Revit2026ParameterCatalog : IRevitParameterCatalog
{
    private readonly Document _document;
    private int _objectsInspected;

    public Revit2026ParameterCatalog(Document document)
    {
        ArgumentGuard.ThrowIfNull(document);
        _document = document;
    }

    public int ObjectsInspected => _objectsInspected;

    public IReadOnlyList<IRevitParameterCatalogEntry> GetParameters()
    {
        var families = new Revit2026FamilyCatalog(_document).GetFamilies();
        var entries = new List<IRevitParameterCatalogEntry>();
        _objectsInspected = 0;

        foreach (var family in families.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            _objectsInspected++;

            foreach (var familyType in family.FamilyTypes.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
            {
                _objectsInspected++;

                foreach (var parameter in familyType.Parameters.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
                {
                    entries.Add(CreateEntry(parameter, family, familyType));
                }
            }
        }

        return entries
            .OrderBy(entry => entry.Parameter.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static RevitParameterCatalogEntry CreateEntry(
        IRevitParameterHandle parameter,
        IRevitFamilyHandle family,
        IRevitFamilyTypeHandle familyType)
    {
        return new RevitParameterCatalogEntry
        {
            Parameter = parameter,
            CategoryName = family.Category?.Name,
            FamilyId = family.Id,
            FamilyName = family.Name,
            FamilyTypeId = familyType.Id,
            FamilyTypeName = familyType.Name
        };
    }
}
