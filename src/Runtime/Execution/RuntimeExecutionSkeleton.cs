using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Runtime.Diagnostics;
using BIMCapabilities.Runtime.Evidence;
using BIMCapabilities.Runtime.Registration;
using ExecutionContextContract = BIMCapabilities.Contracts.Execution.ExecutionContext;

namespace BIMCapabilities.Runtime.Execution;

/// <summary>
/// Composes execution plans and placeholder results without performing engine execution.
/// </summary>
public sealed class RuntimeExecutionSkeleton : IRuntimeExecution
{
    private readonly IRuntimeRegistry _registry;
    private readonly RuntimeDiagnosticsSkeleton _diagnostics;
    private readonly IRuntimeEvidence _evidence;

    public RuntimeExecutionSkeleton(
        IRuntimeRegistry registry,
        RuntimeDiagnosticsSkeleton diagnostics,
        IRuntimeEvidence evidence)
    {
        ArgumentGuard.ThrowIfNull(registry);
        ArgumentGuard.ThrowIfNull(diagnostics);
        ArgumentGuard.ThrowIfNull(evidence);

        _registry = registry;
        _diagnostics = diagnostics;
        _evidence = evidence;
    }

    public ExecutionPlan CreatePlan(ExecutionContextContract context)
    {
        ArgumentGuard.ThrowIfNull(context);

        var steps = context.Rule.Engines
            .OrderBy(engine => engine.Order)
            .Select(engine => new ExecutionStep
            {
                StepId = $"step-{engine.Order:D3}",
                Name = engine.EngineId,
                StepType = "Engine",
                Order = engine.Order,
                Configuration = CreateStepConfiguration(engine)
            })
            .ToArray();

        return new ExecutionPlan
        {
            PlanId = $"plan-{context.CorrelationId}",
            RuleId = context.Rule.Metadata.RuleId,
            RuleVersion = context.Rule.Metadata.RuleVersion,
            ContractVersion = context.Rule.Metadata.ContractVersion,
            RuleSourcePath = context.RuleSourcePath,
            Mode = context.Request.Mode,
            CreatedAt = context.Request.RequestedAt,
            Steps = steps,
            Metadata = new Dictionary<string, string>
            {
                ["scopeType"] = context.Scope.ScopeType,
                ["targetDescription"] = context.Scope.TargetDescription ?? string.Empty,
                ["platform"] = context.Environment.Platform
            }
        };
    }

    public ExecutionResult ComposeResult(ExecutionContextContract context, ExecutionPlan plan)
    {
        ArgumentGuard.ThrowIfNull(context);
        ArgumentGuard.ThrowIfNull(plan);

        VerifyRegisteredEngines(plan);

        _diagnostics.AddPlaceholder(
            context.CorrelationId,
            "Runtime execution pipeline is not implemented. Contract composition completed successfully.");

        return new ExecutionResult
        {
            Status = ExecutionStatus.Pending,
            Diagnostics = _diagnostics.Collection,
            Evidence = _evidence.Collection,
            Summary = new ExecutionSummary
            {
                Status = ExecutionStatus.Pending,
                StartedAt = context.Request.RequestedAt,
                TotalSteps = plan.Steps.Count,
                CompletedSteps = 0,
                FailedSteps = 0,
                SkippedSteps = 0,
                Message = "Runtime execution is not implemented."
            },
            Correlation = new ExecutionCorrelation
            {
                CorrelationId = context.CorrelationId,
                ParentCorrelationId = context.ParentCorrelationId,
                TraceId = context.TraceId,
                PlanId = plan.PlanId
            }
        };
    }

    private void VerifyRegisteredEngines(ExecutionPlan plan)
    {
        foreach (var step in plan.Steps)
        {
            var engineId = step.Configuration?.GetValueOrDefault("engineId");
            if (string.IsNullOrWhiteSpace(engineId))
            {
                continue;
            }

            if (_registry.FindRegistration(engineId) is null)
            {
                _diagnostics.Add(new DiagnosticRecord
                {
                    DiagnosticId = $"runtime-diagnostic-unregistered-{engineId}",
                    Timestamp = DateTimeOffset.UtcNow,
                    Source = new DiagnosticSource
                    {
                        ComponentType = "Runtime",
                        ComponentId = "runtime-skeleton",
                        Operation = "VerifyRegisteredEngines",
                        Code = "EngineNotRegistered"
                    },
                    Category = DiagnosticCategory.Runtime,
                    Severity = DiagnosticSeverity.Warning,
                    Message = $"Engine '{engineId}' is referenced by the plan but is not registered.",
                    StructuredMetadata = new Dictionary<string, string>
                    {
                        ["engineId"] = engineId,
                        ["stepId"] = step.StepId
                    }
                });
            }
        }
    }

    private static IReadOnlyDictionary<string, string> CreateStepConfiguration(BimRuleEngine engine)
    {
        var capabilityIds = engine.Capabilities?
            .Select(capability => capability.AtomId)
            .Where(atomId => !string.IsNullOrWhiteSpace(atomId))
            .ToArray() ?? [];

        return new Dictionary<string, string>
        {
            ["engineId"] = engine.EngineId,
            ["capabilityIds"] = string.Join(",", capabilityIds)
        };
    }
}
