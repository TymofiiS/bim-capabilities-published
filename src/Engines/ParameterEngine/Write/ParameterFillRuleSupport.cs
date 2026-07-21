using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Engines.Parameter.Compliance;

namespace BIMCapabilities.Engines.Parameter.Write;

public static class ParameterFillRuleSupport
{
    private const string FromPrefix = "from:";

    public static IReadOnlyDictionary<string, string> ParseParameterFillRules(string value)
    {
        var fillRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in SplitCommaSeparatedValues(value))
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= token.Length - 1)
            {
                continue;
            }

            var parameterName = token[..separatorIndex].Trim();
            var fillRule = token[(separatorIndex + 1)..].Trim();
            if (parameterName.Length == 0 || fillRule.Length == 0)
            {
                continue;
            }

            fillRules[parameterName] = fillRule;
        }

        return fillRules;
    }

    internal static bool IsSupportedFillRuleSyntax(string fillRule)
    {
        if (!fillRule.StartsWith(FromPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var source = fillRule[FromPrefix.Length..].Trim();
        return source.Length > 0;
    }

    internal static string? ResolveFillValue(
        ParameterComplianceFinding finding,
        ParameterTargetSet targetSet,
        string fillRule)
    {
        if (!fillRule.StartsWith(FromPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var source = fillRule[FromPrefix.Length..].Trim();
        if (source.Equals("FamilyTypeName", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveFamilyTypeName(finding, targetSet);
        }

        if (source.Equals("FamilyName", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveFamilyName(finding, targetSet);
        }

        return ResolveParameterValue(finding, targetSet, source);
    }

    private static string? ResolveFamilyTypeName(
        ParameterComplianceFinding finding,
        ParameterTargetSet targetSet)
    {
        var familyType = targetSet.TargetTypes?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));

        if (familyType is not null)
        {
            return familyType.Name;
        }

        if (string.Equals(finding.ObjectKind, "familyType", StringComparison.OrdinalIgnoreCase))
        {
            return finding.ObjectName;
        }

        return null;
    }

    private static string? ResolveFamilyName(
        ParameterComplianceFinding finding,
        ParameterTargetSet targetSet)
    {
        var family = targetSet.TargetFamilies?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));

        if (family is not null)
        {
            return family.Name;
        }

        var familyType = targetSet.TargetTypes?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));

        if (familyType?.Metadata?.TryGetValue("familyName", out var familyName) == true
            && !string.IsNullOrWhiteSpace(familyName))
        {
            return familyName;
        }

        var instance = targetSet.TargetInstances?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));

        return instance?.FamilyName;
    }

    private static string? ResolveParameterValue(
        ParameterComplianceFinding finding,
        ParameterTargetSet targetSet,
        string sourceParameterName)
    {
        var parameters = CollectObjectParameters(finding, targetSet);
        if (parameters.TryGetValue(sourceParameterName, out var parameter)
            && !string.IsNullOrWhiteSpace(parameter.Value))
        {
            return parameter.Value;
        }

        return null;
    }

    private static Dictionary<string, NormalizedParameter> CollectObjectParameters(
        ParameterComplianceFinding finding,
        ParameterTargetSet targetSet)
    {
        var parameters = new Dictionary<string, NormalizedParameter>(StringComparer.OrdinalIgnoreCase);

        var familyType = targetSet.TargetTypes?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));
        if (familyType is not null)
        {
            AddParameters(parameters, familyType.Parameters);
            return parameters;
        }

        var family = targetSet.TargetFamilies?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));
        if (family is not null)
        {
            AddParameters(parameters, family.Parameters);
            return parameters;
        }

        var instance = targetSet.TargetInstances?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));
        if (instance is not null)
        {
            AddParameters(parameters, instance.Parameters);
        }

        return parameters;
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

    private static IEnumerable<string> SplitCommaSeparatedValues(string value)
    {
        return StringParsing.SplitCommaSeparated(value);
    }
}
