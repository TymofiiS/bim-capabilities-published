using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal sealed class Revit2026FamilyHandle : IRevitFamilyHandle
{
    public Revit2026FamilyHandle(Family family, Document document)
    {
        ArgumentGuard.ThrowIfNull(family);
        ArgumentGuard.ThrowIfNull(document);

        Id = family.Id.ToString();
        Name = family.Name;
        Category = family.FamilyCategory is null ? null : new Revit2026CategoryHandle(family.FamilyCategory);
        var familyParameterContext = Revit2026FamilyParameterContextCollector.Collect(family, document);

        FamilyTypes = family.GetFamilySymbolIds()
            .Select(symbolId => document.GetElement(symbolId))
            .OfType<FamilySymbol>()
            .Select(symbol => new Revit2026FamilyTypeHandle(symbol, document, familyParameterContext))
            .OrderBy(handle => handle.Id, StringComparer.Ordinal)
            .Cast<IRevitFamilyTypeHandle>()
            .ToList();

        Parameters = Revit2026ParameterCollector.MergeParameterNames(
            UnionFamilyParameters(FamilyTypes),
            familyParameterContext.DefinitionNames);
        Relationships = [];
        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["isInPlace"] = family.IsInPlace.ToString().ToLowerInvariant()
        };
    }

    public string Id { get; }

    public string Name { get; }

    public IRevitCategoryHandle? Category { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public IReadOnlyList<IRevitFamilyTypeHandle> FamilyTypes { get; }

    public IReadOnlyList<IRevitParameterHandle> Parameters { get; }

    public IReadOnlyList<IRevitRelationshipHandle> Relationships { get; }

    private static IReadOnlyList<IRevitParameterHandle> UnionFamilyParameters(
        IReadOnlyList<IRevitFamilyTypeHandle> familyTypes)
    {
        var parameters = new Dictionary<string, IRevitParameterHandle>(StringComparer.OrdinalIgnoreCase);

        foreach (var familyType in familyTypes)
        {
            foreach (var parameter in familyType.Parameters)
            {
                if (!parameters.ContainsKey(parameter.Name))
                {
                    parameters.Add(parameter.Name, parameter);
                }
            }
        }

        return parameters.Values
            .OrderBy(parameter => parameter.Id, StringComparer.Ordinal)
            .ToList();
    }
}
