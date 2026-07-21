using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

namespace BIMCapabilities.Composition.Capabilities.Handlers;

internal sealed class ReportComplianceCapabilityHandler : IBimRuleCapabilityHandler
{
    public string HandlerId => CapabilityHandlerIds.ReportCompliance;

    public string EngineId => "report-engine";

    public string CapabilityId => "report.compliance";

    public void ContributeToExecutionPlan(
        BimRuleCapabilityInterpretationContext context,
        IBimRuleExecutionPlanBuilder builder)
    {
        builder.EnableReportGeneration();
    }
}
