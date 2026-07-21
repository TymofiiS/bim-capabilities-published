using System.Globalization;
using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal static class Revit2026ParameterCollector
{
    internal static IReadOnlyList<IRevitParameterHandle> Collect(Element element)
    {
        return element
            .GetOrderedParameters()
            .Select(parameter => new Revit2026ParameterHandle(parameter))
            .OrderBy(handle => handle.Id, StringComparer.Ordinal)
            .Cast<IRevitParameterHandle>()
            .ToList();
    }

    internal static IReadOnlyList<IRevitParameterHandle> MergeParameterNames(
        IReadOnlyList<IRevitParameterHandle> existingParameters,
        IEnumerable<string> additionalParameterNames)
    {
        var parameters = existingParameters.ToDictionary(
            parameter => parameter.Name,
            StringComparer.OrdinalIgnoreCase);

        foreach (var parameterName in additionalParameterNames)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                continue;
            }

            if (!parameters.ContainsKey(parameterName))
            {
                parameters.Add(parameterName, new Revit2026SyntheticParameterHandle(parameterName));
            }
        }

        return parameters.Values
            .OrderBy(parameter => parameter.Id, StringComparer.Ordinal)
            .ToList();
    }

    internal static IReadOnlyList<IRevitParameterHandle> MergeParameters(
        IReadOnlyList<IRevitParameterHandle> existingParameters,
        IEnumerable<IRevitParameterHandle> additionalParameters)
    {
        var parameters = existingParameters.ToDictionary(
            parameter => parameter.Name,
            StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in additionalParameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                continue;
            }

            if (!parameters.TryGetValue(parameter.Name, out var existing))
            {
                parameters[parameter.Name] = parameter;
                continue;
            }

            if (ShouldReplaceParameter(existing, parameter))
            {
                parameters[parameter.Name] = parameter;
            }
        }

        return parameters.Values
            .OrderBy(parameter => parameter.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static bool ShouldReplaceParameter(
        IRevitParameterHandle existing,
        IRevitParameterHandle candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate.Value) && string.IsNullOrWhiteSpace(existing.Value))
        {
            return true;
        }

        if (IsFamilyDefinitionPlaceholder(existing) && !IsFamilyDefinitionPlaceholder(candidate))
        {
            return true;
        }

        return false;
    }

    private static bool IsFamilyDefinitionPlaceholder(IRevitParameterHandle parameter)
    {
        return string.Equals(
            parameter.Metadata?.GetValueOrDefault("parameterKind"),
            "familyDefinition",
            StringComparison.Ordinal);
    }
}

internal sealed class Revit2026ParameterHandle : IRevitParameterHandle
{
    public Revit2026ParameterHandle(Parameter parameter)
    {
        ArgumentGuard.ThrowIfNull(parameter);

        Id = parameter.Id.ToString();
        Name = parameter.Definition.Name;
        Value = ReadValue(parameter);
        StorageType = parameter.StorageType.ToString();
        IsSharedParameter = parameter.Definition is ExternalDefinition;

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);

        if (parameter.Definition is ExternalDefinition externalDefinition)
        {
            metadata["guid"] = externalDefinition.GUID.ToString();
            metadata["parameterKind"] = "shared";
        }
        else if (parameter.Definition is InternalDefinition internalDefinition &&
                 internalDefinition.BuiltInParameter != BuiltInParameter.INVALID)
        {
            metadata["builtInParameter"] = internalDefinition.BuiltInParameter.ToString();
            metadata["parameterKind"] = "builtIn";
        }
        else
        {
            metadata["parameterKind"] = "family";
        }

        Metadata = metadata.Count > 0 ? metadata : null;
    }

    public string Id { get; }

    public string Name { get; }

    public string? Value { get; }

    public string StorageType { get; }

    public bool IsSharedParameter { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }

    private static string? ReadValue(Parameter parameter)
    {
        if (!parameter.HasValue)
        {
            return null;
        }

        return parameter.StorageType switch
        {
            Autodesk.Revit.DB.StorageType.String => parameter.AsString(),
            Autodesk.Revit.DB.StorageType.Integer => parameter.AsInteger().ToString(CultureInfo.InvariantCulture),
            Autodesk.Revit.DB.StorageType.Double => parameter.AsDouble().ToString(CultureInfo.InvariantCulture),
            Autodesk.Revit.DB.StorageType.ElementId => parameter.AsElementId().ToString(),
            _ => parameter.AsValueString()
        };
    }
}

internal sealed class Revit2026SyntheticParameterHandle : IRevitParameterHandle
{
    public Revit2026SyntheticParameterHandle(string name, string? value = null)
    {
        Name = name;
        Id = value is null
            ? $"family-definition:{name}"
            : $"family-default:{name}";
        Value = value;
        StorageType = Autodesk.Revit.DB.StorageType.String.ToString();
    }

    public string Id { get; }

    public string Name { get; }

    public string? Value { get; }

    public string StorageType { get; }

    public bool IsSharedParameter => true;

    public IReadOnlyDictionary<string, string>? Metadata { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["parameterKind"] = "familyDefinition"
        };
}
