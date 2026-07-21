using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

namespace BIMCapabilities.Composition.Capabilities.Handlers;

internal sealed class FamilyImportedCadCapabilityHandler : CapabilityHandlerSupport, IBimRuleCapabilityHandler
{
    private const string ExcludeImportedCadCategoriesKey = "excludeImportedCad.categories";

    public string HandlerId => CapabilityHandlerIds.FamilyImportedCad;

    public string EngineId => "family-engine";

    public string CapabilityId => "family.imported-cad";

    public void ContributeToExecutionPlan(
        BimRuleCapabilityInterpretationContext context,
        IBimRuleExecutionPlanBuilder builder)
    {
        builder.EnableImportedCadExclusion();

        foreach (var categoryName in ParseCommaSeparatedValues(
                     GetConfigurationValue(context.MergedConfiguration, ExcludeImportedCadCategoriesKey)))
        {
            builder.AddCategory(categoryName);
            builder.SetExcludeImportedCad(categoryName, exclude: true);
        }
    }
}
