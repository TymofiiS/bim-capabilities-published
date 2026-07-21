using BIMCapabilities.Composition.Capabilities;
using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

namespace BIMCapabilities.Composition.Interpretation;

/// <summary>
/// Interprets BIMRule capability references and configuration into an execution plan.
/// </summary>
internal static class BimRuleExecutionInterpreter
{
    internal static BimRuleExecutionPlan Interpret(BimRule rule)
    {
        return Interpret(rule, CapabilityPlatform.Default);
    }

    internal static BimRuleExecutionPlan Interpret(BimRule rule, CapabilityPlatform platform)
    {
        ArgumentGuard.ThrowIfNull(rule);
        ArgumentGuard.ThrowIfNull(platform);

        var builder = new BimRuleExecutionPlanBuilder(rule.Metadata.RuleId);
        var mergedConfiguration = MergeConfiguration(rule);

        foreach (var (engineId, capabilityReference) in EnumerateCapabilityReferences(rule))
        {
            if (!platform.Registry.TryGetDefinition(engineId, capabilityReference.AtomId, out var definition) ||
                definition is null)
            {
                continue;
            }

            var handler = platform.Handlers.Resolve(definition.HandlerId);
            handler.ContributeToExecutionPlan(
                new BimRuleCapabilityInterpretationContext
                {
                    Rule = rule,
                    EngineId = engineId,
                    CapabilityReference = capabilityReference,
                    MergedConfiguration = mergedConfiguration
                },
                builder);
        }

        return builder.Build();
    }

    internal static string? ResolveSharedParameterFilePath(BimRule rule, string? overridePath)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        return rule.ExternalReferences?
            .FirstOrDefault(reference =>
                string.Equals(reference.ReferenceType, "SharedParameterFile", StringComparison.OrdinalIgnoreCase))
            ?.Location;
    }

    private static IEnumerable<(string EngineId, BimRuleCapabilityReference CapabilityReference)> EnumerateCapabilityReferences(
        BimRule rule)
    {
        if (rule.Engines is null)
        {
            yield break;
        }

        foreach (var engine in rule.Engines.OrderBy(engine => engine.Order))
        {
            if (engine.Capabilities is null)
            {
                continue;
            }

            foreach (var capabilityReference in engine.Capabilities)
            {
                if (string.IsNullOrWhiteSpace(capabilityReference.AtomId))
                {
                    continue;
                }

                yield return (engine.EngineId, capabilityReference);
            }
        }
    }

    private static Dictionary<string, string> MergeConfiguration(BimRule rule)
    {
        var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var engine in rule.Engines ?? [])
        {
            if (engine.Capabilities is null)
            {
                continue;
            }

            foreach (var capability in engine.Capabilities)
            {
                if (capability.Configuration is null)
                {
                    continue;
                }

                foreach (var entry in capability.Configuration)
                {
                    configuration[entry.Key] = entry.Value;
                }
            }
        }

        return configuration;
    }
}
