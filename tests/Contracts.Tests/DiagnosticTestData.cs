using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Contracts.Tests;

internal static class DiagnosticTestData
{
    internal static DiagnosticRecord CreateRuntimeExecutionFailure()
    {
        return new DiagnosticRecord
        {
            DiagnosticId = "diagnostic-001",
            Timestamp = new DateTimeOffset(2026, 6, 19, 16, 0, 0, TimeSpan.Zero),
            Source = new DiagnosticSource
            {
                ComponentType = "Runtime",
                ComponentId = "execution-pipeline",
                Operation = "ExecuteEngineStep",
                Code = "EngineStepFailed"
            },
            Category = DiagnosticCategory.Execution,
            Severity = DiagnosticSeverity.Error,
            Message = "Engine step failed before evidence was produced.",
            StructuredMetadata = new Dictionary<string, string>
            {
                ["engineId"] = "parameter-engine",
                ["stepOrder"] = "2",
                ["failureReason"] = "MissingAdapterCapability"
            },
            Context = new DiagnosticContext
            {
                RuleId = "STD-ARC-OPENINGS-V01",
                RuleSourcePath = @"D:\Demo\Rules\STD-ARC-OPENINGS-V01.bimrule",
                ExecutionMode = ExecutionMode.Validation,
                EngineId = "parameter-engine",
                CapabilityId = "parameter.existence",
                CorrelationId = "corr-001",
                ParentCorrelationId = "parent-corr-001",
                TraceId = "trace-001"
            },
            CorrelationId = "corr-001",
            ParentCorrelationId = "parent-corr-001",
            TraceId = "trace-001"
        };
    }

    internal static DiagnosticCollection CreateDemoCollection()
    {
        return new DiagnosticCollection
        {
            CollectionId = "diagnostics-001",
            CorrelationId = "corr-001",
            Records =
            [
                CreateRuntimeExecutionFailure(),
                new DiagnosticRecord
                {
                    DiagnosticId = "diagnostic-002",
                    Timestamp = new DateTimeOffset(2026, 6, 19, 16, 1, 0, TimeSpan.Zero),
                    Source = new DiagnosticSource
                    {
                        ComponentType = "Launcher",
                        ComponentId = "revit-launcher",
                        Operation = "PresentDiagnostics",
                        Code = "DiagnosticsPresented"
                    },
                    Category = DiagnosticCategory.Launcher,
                    Severity = DiagnosticSeverity.Information,
                    Message = "Diagnostics were presented to the user.",
                    CorrelationId = "corr-001"
                }
            ]
        };
    }
}
