using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Contracts.Tests;

internal static class RuntimeExecutionTestData
{
    internal static ExecutionPlan CreateDemoPlan()
    {
        return new ExecutionPlan
        {
            PlanId = "plan-001",
            RuleId = "STD-ARC-OPENINGS-V01",
            RuleVersion = "V01",
            ContractVersion = "1.0",
            RuleSourcePath = @"D:\Demo\Rules\STD-ARC-OPENINGS-V01.bimrule",
            Mode = ExecutionMode.Validation,
            CreatedAt = new DateTimeOffset(2026, 6, 19, 17, 0, 0, TimeSpan.Zero),
            Metadata = new Dictionary<string, string>
            {
                ["targetPlatform"] = "Revit",
                ["failureBehavior"] = "StopOnFirstUnrecoverableFailure"
            },
            Steps =
            [
                new ExecutionStep
                {
                    StepId = "step-001",
                    Name = "Naming Validation",
                    StepType = "Engine",
                    Order = 1,
                    Configuration = new Dictionary<string, string>
                    {
                        ["engineId"] = "naming-engine",
                        ["capabilityId"] = "naming.prefix.validation"
                    }
                },
                new ExecutionStep
                {
                    StepId = "step-002",
                    Name = "Parameter Validation",
                    StepType = "Engine",
                    Order = 2,
                    Configuration = new Dictionary<string, string>
                    {
                        ["engineId"] = "parameter-engine",
                        ["capabilityId"] = "parameter.existence"
                    }
                }
            ]
        };
    }

    internal static ExecutionResult CreateCompletedResult()
    {
        return new ExecutionResult
        {
            Status = ExecutionStatus.Completed,
            Diagnostics = DiagnosticTestData.CreateDemoCollection(),
            Evidence = EvidenceTestData.CreateDemoCollection(),
            Summary = new ExecutionSummary
            {
                Status = ExecutionStatus.Completed,
                StartedAt = new DateTimeOffset(2026, 6, 19, 17, 0, 0, TimeSpan.Zero),
                CompletedAt = new DateTimeOffset(2026, 6, 19, 17, 5, 0, TimeSpan.Zero),
                TotalSteps = 2,
                CompletedSteps = 2,
                FailedSteps = 0,
                SkippedSteps = 0,
                Message = "Execution completed successfully."
            },
            Correlation = new ExecutionCorrelation
            {
                CorrelationId = "corr-001",
                ParentCorrelationId = "parent-corr-001",
                TraceId = "trace-001",
                PlanId = "plan-001"
            }
        };
    }
}
