using System.Text.Json;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Tests;

internal static class BimRuleTestData
{
    internal static readonly JsonSerializerOptions JsonOptions = BimRuleSerialization.Options;

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
            ExternalReferences =
            [
                new BimRuleExternalReference
                {
                    ReferenceType = "SharedParameterFile",
                    Location = @"D:\Demo\CompanySharedParameters.txt",
                    Purpose = "Resolve required shared parameters.",
                    IsRequired = true,
                    ConsumerEngine = "parameter-engine"
                }
            ],
            Engines =
            [
                new BimRuleEngine
                {
                    EngineId = "naming-engine",
                    Order = 1,
                    Capabilities =
                    [
                        new BimRuleCapabilityReference { AtomId = "naming.prefix.validation" }
                    ]
                },
                new BimRuleEngine
                {
                    EngineId = "parameter-engine",
                    Order = 2,
                    Capabilities =
                    [
                        new BimRuleCapabilityReference { AtomId = "parameter.existence" }
                    ]
                },
                new BimRuleEngine
                {
                    EngineId = "family-engine",
                    Order = 3,
                    Capabilities =
                    [
                        new BimRuleCapabilityReference { AtomId = "family.imported-cad" }
                    ]
                },
                new BimRuleEngine
                {
                    EngineId = "report-engine",
                    Order = 4,
                    Capabilities =
                    [
                        new BimRuleCapabilityReference { AtomId = "report.compliance" }
                    ]
                }
            ],
            Execution = new BimRuleExecution
            {
                TargetPlatform = "Revit",
                ExecutionMode = "Validation",
                ValidationEnabled = true,
                FixEnabled = false,
                DryRun = false,
                RequireUserApprovalBeforeModification = false,
                FailureBehavior = "StopOnFirstUnrecoverableFailure"
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
}
