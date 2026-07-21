namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

/// <summary>
/// Interprets one registered capability into execution-plan contributions.
/// </summary>
public interface IBimRuleCapabilityHandler
{
    string HandlerId { get; }

    string EngineId { get; }

    string CapabilityId { get; }

    void ContributeToExecutionPlan(
        BimRuleCapabilityInterpretationContext context,
        IBimRuleExecutionPlanBuilder builder);
}
