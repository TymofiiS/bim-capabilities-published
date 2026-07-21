using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Tests;

internal static class ExecutionContextTestData
{
    internal static Contracts.Execution.ExecutionContext CreateDemoContext()
    {
        return new Contracts.Execution.ExecutionContext
        {
            Rule = BimRuleTestData.CreateDemoRule(),
            RuleSourcePath = @"D:\Demo\Rules\STD-ARC-OPENINGS-V01.bimrule",
            Request = new ExecutionRequest
            {
                Mode = ExecutionMode.Validation,
                DryRun = false,
                RequireUserApprovalBeforeModification = false,
                RequestedAt = new DateTimeOffset(2026, 6, 19, 12, 0, 0, TimeSpan.Zero),
                RequestedBy = "DemoUser"
            },
            Scope = new ExecutionScope
            {
                ScopeType = "Category",
                TargetDescription = "All Doors",
                Criteria = new Dictionary<string, string>
                {
                    ["category"] = "Doors"
                }
            },
            Environment = new ExecutionEnvironment
            {
                Platform = "Revit",
                PlatformVersion = "2026",
                SessionId = "session-001",
                ModelIdentifier = "model-001",
                ModelName = "Hotel Sample.rvt"
            },
            CorrelationId = "corr-001",
            ParentCorrelationId = "parent-corr-001",
            TraceId = "trace-001"
        };
    }
}
