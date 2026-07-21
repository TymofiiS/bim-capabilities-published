using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Engines.Registration;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Runtime.Tests;

internal static class RuntimeIntegrationTestData
{
    internal static BimRule CreateDemoRule()
    {
        return new BimRule
        {
            Metadata = new BimRuleMetadata
            {
                RuleId = "STD-ARC-OPENINGS-V01",
                Name = "STD-ARC-OPENINGS-V01",
                RuleVersion = "V01",
                ContractVersion = "1.0",
                Description = "Door and window family acceptance standard.",
                Domain = "Openings",
                Status = "Approved",
                Author = "Demo",
                CreatedAt = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)
            },
            Engines =
            [
                new BimRuleEngine
                {
                    EngineId = "naming-engine",
                    Order = 1,
                    Capabilities = [new BimRuleCapabilityReference { AtomId = "naming.prefix.validation" }]
                },
                new BimRuleEngine
                {
                    EngineId = "parameter-engine",
                    Order = 2,
                    Capabilities = [new BimRuleCapabilityReference { AtomId = "parameter.existence" }]
                },
                new BimRuleEngine
                {
                    EngineId = "family-engine",
                    Order = 3,
                    Capabilities = [new BimRuleCapabilityReference { AtomId = "family.imported-cad" }]
                },
                new BimRuleEngine
                {
                    EngineId = "report-engine",
                    Order = 4,
                    Capabilities = [new BimRuleCapabilityReference { AtomId = "report.compliance" }]
                }
            ],
            Execution = new BimRuleExecution
            {
                TargetPlatform = "Revit",
                ExecutionMode = "Validation"
            },
            Report = new BimRuleReport
            {
                GenerateHtmlReport = true,
                GenerateJsonReport = true,
                IncludeEvidence = true,
                ReportTitle = "Openings Compliance Report",
                ComplianceSummaryProfile = "Compliance",
                ResultGrouping = "Engine"
            }
        };
    }

    internal static Contracts.Execution.ExecutionContext CreateExecutionContext()
    {
        return new Contracts.Execution.ExecutionContext
        {
            Rule = CreateDemoRule(),
            RuleSourcePath = @"D:\Demo\Rules\STD-ARC-OPENINGS-V01.bimrule",
            Request = new ExecutionRequest
            {
                Mode = ExecutionMode.Validation,
                RequestedAt = new DateTimeOffset(2026, 6, 19, 19, 0, 0, TimeSpan.Zero),
                RequestedBy = "DemoUser"
            },
            Scope = new ExecutionScope
            {
                ScopeType = "Category",
                TargetDescription = "All Doors"
            },
            Environment = new ExecutionEnvironment
            {
                Platform = "Revit",
                PlatformVersion = "2026",
                ModelName = "Hotel Sample.rvt"
            },
            CorrelationId = "corr-runtime-001",
            TraceId = "trace-runtime-001"
        };
    }

    internal static IReadOnlyList<EngineRegistration> CreateMvpRegistrations()
    {
        var registeredAt = new DateTimeOffset(2026, 6, 19, 18, 0, 0, TimeSpan.Zero);

        EngineRegistration CreateRegistration(string engineId, EngineType engineType, string capabilityName)
        {
            return new EngineRegistration
            {
                RegisteredAt = registeredAt,
                Engine = new EngineDefinition
                {
                    EngineId = engineId,
                    Name = engineId,
                    EngineType = engineType,
                    Version = new EngineVersion
                    {
                        Version = "1.0",
                        ConfigurationSchemaVersion = "1.0",
                        RuntimeCompatibilityVersion = "1.0"
                    },
                    Capabilities =
                    [
                        new EngineCapability
                        {
                            CapabilityName = capabilityName,
                            CapabilityVersion = "1.0",
                            CapabilityCategory = "Validation"
                        }
                    ]
                }
            };
        }

        return
        [
            CreateRegistration("naming-engine", EngineType.Naming, "naming.prefix.validation"),
            CreateRegistration("parameter-engine", EngineType.Parameter, "parameter.existence"),
            CreateRegistration("family-engine", EngineType.Family, "family.imported-cad"),
            CreateRegistration("report-engine", EngineType.Report, "report.compliance")
        ];
    }

    internal static EvidenceRecord CreateSampleEvidence()
    {
        return new EvidenceRecord
        {
            EvidenceId = "runtime-evidence-001",
            Timestamp = new DateTimeOffset(2026, 6, 19, 19, 1, 0, TimeSpan.Zero),
            Source = new EvidenceSource
            {
                EngineId = "parameter-engine",
                AtomId = "parameter.existence",
                RuleId = "STD-ARC-OPENINGS-V01",
                CapabilityId = "parameter.existence"
            },
            Category = EvidenceCategory.Validation,
            Severity = EvidenceSeverity.Info,
            Message = "Sample evidence attached during runtime contract composition."
        };
    }

    internal static DiagnosticRecord CreateSampleDiagnostic()
    {
        return new DiagnosticRecord
        {
            DiagnosticId = "runtime-diagnostic-sample-001",
            Timestamp = new DateTimeOffset(2026, 6, 19, 19, 1, 0, TimeSpan.Zero),
            Source = new DiagnosticSource
            {
                ComponentType = "Runtime",
                ComponentId = "runtime-skeleton",
                Code = "SampleDiagnostic"
            },
            Category = DiagnosticCategory.Runtime,
            Severity = DiagnosticSeverity.Information,
            Message = "Sample diagnostic attached during runtime contract composition.",
            CorrelationId = "corr-runtime-001"
        };
    }
}
