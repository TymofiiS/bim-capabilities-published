using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal sealed class Revit2026FamilyTypeHandle : IRevitFamilyTypeHandle
{
    public Revit2026FamilyTypeHandle(
        FamilySymbol symbol,
        Document document,
        Revit2026FamilyParameterContext familyParameterContext)
    {
        ArgumentGuard.ThrowIfNull(symbol);
        ArgumentGuard.ThrowIfNull(document);
        ArgumentGuard.ThrowIfNull(familyParameterContext);

        Id = symbol.Id.ToString();
        Name = symbol.Name;
        Parameters = BuildParameters(symbol, document, familyParameterContext);
        var placedInstanceCount = new FilteredElementCollector(document)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .Count(instance => instance.Symbol.Id == symbol.Id);

        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["placedInstanceCount"] = placedInstanceCount.ToString()
        };
    }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public IReadOnlyList<IRevitParameterHandle> Parameters { get; }

    internal static IReadOnlyList<string> CollectFamilyParameterDefinitionNames(Family family, Document document)
    {
        return Revit2026FamilyParameterContextCollector.Collect(family, document).DefinitionNames;
    }

    private static IReadOnlyList<IRevitParameterHandle> BuildParameters(
        FamilySymbol symbol,
        Document document,
        Revit2026FamilyParameterContext familyParameterContext)
    {
        var parameters = Revit2026ParameterCollector.MergeParameterNames(
            Revit2026ParameterCollector.Collect(symbol),
            familyParameterContext.DefinitionNames);

        parameters = Revit2026ParameterCollector.MergeParameters(
            parameters,
            CreateFamilyTypeDefaultParameters(symbol.Name, familyParameterContext));

        return Revit2026ParameterCollector.MergeParameters(
            parameters,
            CollectPlacedInstanceParameters(document, symbol));
    }

    private static IEnumerable<IRevitParameterHandle> CreateFamilyTypeDefaultParameters(
        string typeName,
        Revit2026FamilyParameterContext familyParameterContext)
    {
        foreach (var (parameterName, value) in familyParameterContext.GetValuesForType(typeName))
        {
            yield return new Revit2026SyntheticParameterHandle(parameterName, value);
        }
    }

    private static IReadOnlyList<IRevitParameterHandle> CollectPlacedInstanceParameters(
        Document document,
        FamilySymbol symbol)
    {
        var instance = new FilteredElementCollector(document)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .FirstOrDefault(candidate => candidate.Symbol.Id == symbol.Id);

        return instance is null
            ? []
            : Revit2026ParameterCollector.Collect(instance);
    }
}
