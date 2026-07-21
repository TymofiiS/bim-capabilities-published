using BIMCapabilities.Composition.Interpretation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

namespace BIMCapabilities.Composition.Capabilities.Handlers;

internal sealed class NamingPrefixValidationCapabilityHandler : CapabilityHandlerSupport, IBimRuleCapabilityHandler
{
    private const string PrefixSuffix = ".prefix";
    private const string PrefixFixSuffix = ".prefixFix";

    public string HandlerId => CapabilityHandlerIds.NamingPrefixValidation;

    public string EngineId => "naming-engine";

    public string CapabilityId => "naming.prefix.validation";

    public void ContributeToExecutionPlan(
        BimRuleCapabilityInterpretationContext context,
        IBimRuleExecutionPlanBuilder builder)
    {
        builder.EnableNamingCompliance();

        foreach (var entry in context.MergedConfiguration)
        {
            if (entry.Key.EndsWith(PrefixFixSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var categoryName = entry.Key[..^PrefixFixSuffix.Length];
                builder.AddCategory(categoryName);
                builder.SetPrefixFixScope(categoryName, PrefixFixScopeSupport.Parse(entry.Value));
                continue;
            }

            if (!entry.Key.EndsWith(PrefixSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var prefixCategoryName = entry.Key[..^PrefixSuffix.Length];
            builder.AddCategory(prefixCategoryName);

            if (!string.IsNullOrWhiteSpace(entry.Value))
            {
                builder.SetRequiredPrefix(prefixCategoryName, entry.Value);
            }
        }
    }
}
