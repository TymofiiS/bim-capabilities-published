using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Parameter;

namespace BIMCapabilities.Composition.Mapping;

internal static class TargetSetMapper
{
    internal static NamingTargetSet ToNamingTargetSet(FamilyTargetSet source)
    {
        return new NamingTargetSet
        {
            TargetSetId = source.TargetSetId,
            TargetFamilies = source.Families,
            TargetTypes = source.FamilyTypes,
            Categories = source.Categories,
            SelectionMetadata = source.Metadata
        };
    }

    internal static ParameterTargetSet ToParameterTargetSet(FamilyTargetSet source)
    {
        return new ParameterTargetSet
        {
            TargetSetId = source.TargetSetId,
            TargetFamilies = source.Families,
            TargetTypes = source.FamilyTypes,
            TargetInstances = source.PlacedInstances,
            TargetParameters = CollectParameters(source),
            SelectionMetadata = source.Metadata
        };
    }

    private static IReadOnlyList<NormalizedParameter> CollectParameters(FamilyTargetSet source)
    {
        var parameters = new Dictionary<string, NormalizedParameter>(StringComparer.OrdinalIgnoreCase);

        foreach (var family in source.Families ?? [])
        {
            AddParameters(parameters, family.Parameters);

            foreach (var familyType in family.FamilyTypes ?? [])
            {
                AddParameters(parameters, familyType.Parameters);
            }
        }

        return parameters.Values
            .OrderBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(parameter => parameter.Identifier.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddParameters(
        IDictionary<string, NormalizedParameter> parameters,
        IReadOnlyList<NormalizedParameter>? candidates)
    {
        if (candidates is null)
        {
            return;
        }

        foreach (var parameter in candidates)
        {
            if (!parameters.ContainsKey(parameter.Name))
            {
                parameters.Add(parameter.Name, parameter);
            }
        }
    }
}
