using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

using BIMCapabilities.Engines.Parameter.Write;

namespace BIMCapabilities.Composition.Capabilities.Handlers;

internal sealed class ParameterExistenceCapabilityHandler : CapabilityHandlerSupport, IBimRuleCapabilityHandler
{
    private const string ParametersSuffix = ".parameters";
    private const string ParameterDefaultsSuffix = ".parameterDefaults";
    private const string ParameterFillRulesSuffix = ".parameterFillRules";
    private const string ParameterBindingSuffix = ".parameterBinding";

    private static readonly HashSet<string> KnownSharedParameterNames = new(StringComparer.Ordinal)
    {
        "FireRating",
        "AcousticRating",
        "Manufacturer"
    };

    public string HandlerId => CapabilityHandlerIds.ParameterExistence;

    public string EngineId => "parameter-engine";

    public string CapabilityId => "parameter.existence";

    public void ContributeToExecutionPlan(
        BimRuleCapabilityInterpretationContext context,
        IBimRuleExecutionPlanBuilder builder)
    {
        builder.EnableParameterCompliance();

        foreach (var entry in context.MergedConfiguration)
        {
            if (entry.Key.EndsWith(ParametersSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var categoryName = entry.Key[..^ParametersSuffix.Length];
                builder.AddCategory(categoryName);
                builder.SetRequiredParameters(categoryName, ParseCommaSeparatedValues(entry.Value));
                continue;
            }

            if (entry.Key.EndsWith(ParameterDefaultsSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var categoryName = entry.Key[..^ParameterDefaultsSuffix.Length];
                builder.AddCategory(categoryName);
                builder.SetParameterDefaults(categoryName, ParseParameterDefaults(entry.Value));
                continue;
            }

            if (entry.Key.EndsWith(ParameterFillRulesSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var categoryName = entry.Key[..^ParameterFillRulesSuffix.Length];
                builder.AddCategory(categoryName);
                builder.SetParameterFillRules(categoryName, ParameterFillRuleSupport.ParseParameterFillRules(entry.Value));
                continue;
            }

            if (entry.Key.EndsWith(ParameterBindingSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var categoryName = entry.Key[..^ParameterBindingSuffix.Length];
                builder.AddCategory(categoryName);
                builder.SetParameterBindings(categoryName, ParseParameterBindings(entry.Value));
            }
        }
    }

    internal static IReadOnlyDictionary<string, bool> ParseParameterBindings(string value)
    {
        var bindings = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in ParseCommaSeparatedValues(value))
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= token.Length - 1)
            {
                continue;
            }

            var parameterName = token[..separatorIndex].Trim();
            var bindingValue = token[(separatorIndex + 1)..].Trim();
            if (parameterName.Length == 0)
            {
                continue;
            }

            bindings[parameterName] = IsInstanceBinding(bindingValue);
        }

        return bindings;
    }

    internal static bool IsInstanceBinding(string bindingValue)
    {
        if (string.Equals(bindingValue, "instance", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(bindingValue, "type", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(bindingValue))
        {
            return false;
        }

        throw new InvalidOperationException(
            $"Unsupported parameter binding '{bindingValue}'. Use 'type' or 'instance'.");
    }

    internal static IReadOnlyDictionary<string, string> ParseParameterDefaults(string value)
    {
        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in ParseCommaSeparatedValues(value))
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= token.Length - 1)
            {
                continue;
            }

            var parameterName = token[..separatorIndex].Trim();
            var defaultValue = token[(separatorIndex + 1)..].Trim();
            if (parameterName.Length == 0)
            {
                continue;
            }

            defaults[parameterName] = defaultValue;
        }

        return defaults;
    }

    internal static IReadOnlyList<string> InferSharedParameterNames(IReadOnlyList<string> requiredParameterNames)
    {
        return requiredParameterNames
            .Where(name => KnownSharedParameterNames.Contains(name))
            .ToArray();
    }
}
