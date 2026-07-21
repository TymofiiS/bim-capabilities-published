using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Tests;

internal static class NamingEngineTestData
{
    internal const string DoorPrefix = "DR_";
    internal const string WindowPrefix = "WN_";

    internal static NamingTargetSet CreateDoorTargetSet()
    {
        return new NamingTargetSet
        {
            TargetSetId = "target-set-doors-001",
            TargetFamilies =
            [
                new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier
                    {
                        Id = "family-001",
                        Kind = "family",
                        Scope = "project-document"
                    },
                    Name = "DR_HTL_Door_01",
                    Category = CreateDoorsCategory()
                }
            ],
            TargetTypes =
            [
                new NormalizedFamilyType
                {
                    Identity = new NormalizedIdentifier { Id = "family-type-001", Kind = "familyType" },
                    Name = "DR_HTL_Door_01_900x2100"
                }
            ],
            Categories = [CreateDoorsCategory()],
            SelectionMetadata = new Dictionary<string, string>
            {
                ["scope"] = "all-door-families",
                ["sourceEngine"] = "family-engine"
            }
        };
    }

    internal static NamingTargetSet CreateWindowTargetSet()
    {
        return new NamingTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetFamilies =
            [
                new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier
                    {
                        Id = "family-002",
                        Kind = "family",
                        Scope = "project-document"
                    },
                    Name = "WN_HTL_Window_01",
                    Category = CreateWindowsCategory()
                }
            ],
            TargetTypes =
            [
                new NormalizedFamilyType
                {
                    Identity = new NormalizedIdentifier { Id = "family-type-003", Kind = "familyType" },
                    Name = "WN_HTL_Window_01_1200x1200"
                }
            ],
            Categories = [CreateWindowsCategory()],
            SelectionMetadata = new Dictionary<string, string>
            {
                ["scope"] = "all-window-families",
                ["sourceEngine"] = "family-engine"
            }
        };
    }

    internal static NamingValidationCriteria CreateDoorPrefixCriteria()
    {
        return new NamingValidationCriteria
        {
            RequiredPrefix = DoorPrefix,
            Rules =
            [
                new NamingCriteriaDefinition
                {
                    RequiredPrefix = DoorPrefix,
                    CategoryScope = new NamingCategoryScopeCriteria
                    {
                        CategoryNames = ["Doors"]
                    },
                    ObjectScope = new NamingObjectScopeCriteria
                    {
                        Scope = NamingEngineObjectScope.Family,
                        FamilyIdentifiers = ["family-001"]
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["requirementSource"] = "bimrule"
                    }
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["validationPurpose"] = "door-prefix-compliance"
            }
        };
    }

    internal static NamingValidationCriteria CreateWindowPrefixCriteria()
    {
        return new NamingValidationCriteria
        {
            RequiredPrefix = WindowPrefix,
            Rules =
            [
                new NamingCriteriaDefinition
                {
                    RequiredPrefix = WindowPrefix,
                    CategoryScope = new NamingCategoryScopeCriteria
                    {
                        CategoryNames = ["Windows"]
                    },
                    ObjectScope = new NamingObjectScopeCriteria
                    {
                        Scope = NamingEngineObjectScope.Family,
                        FamilyIdentifiers = ["family-002"]
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["requirementSource"] = "bimrule"
                    }
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["validationPurpose"] = "window-prefix-compliance"
            }
        };
    }

    internal static NamingValidationCriteria CreatePatternCriteria()
    {
        return new NamingValidationCriteria
        {
            NamingPattern = "{Prefix}_{Discipline}_{Element}_{Variant}",
            RegularExpression = @"^[A-Z]{2}_[A-Z0-9_]+$",
            CaseRule = NamingCaseRule.PascalCase,
            CustomRuleIdentifier = "client.openings.naming-pattern",
            Rules =
            [
                new NamingCriteriaDefinition
                {
                    NamingPattern = "{Prefix}_{Discipline}_{Element}_{Variant}",
                    RegularExpression = @"^[A-Z]{2}_[A-Z0-9_]+$",
                    CaseRule = NamingCaseRule.PascalCase,
                    CustomRuleIdentifier = "client.openings.naming-pattern"
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["validationPurpose"] = "pattern-compliance"
            }
        };
    }

    internal static NamingValidationRequest CreateDoorValidationRequest()
    {
        return new NamingValidationRequest
        {
            TargetSet = CreateDoorTargetSet(),
            Criteria = CreateDoorPrefixCriteria(),
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-naming-engine-001",
            Metadata = new Dictionary<string, string>
            {
                ["engineOperation"] = "validation"
            }
        };
    }

    internal static NamingValidationResult CreateDoorValidationResult()
    {
        return new NamingValidationResult
        {
            Findings =
            [
                new NamingValidationFinding
                {
                    ObjectId = "family-001",
                    ObjectKind = "family",
                    ObjectName = "DR_HTL_Door_01",
                    Passed = true,
                    Message = "Family name starts with required prefix 'DR_'.",
                    RuleIdentifier = "door-prefix"
                }
            ],
            Evidence =
            [
                new EvidenceRecord
                {
                    EvidenceId = "evidence-naming-001",
                    Timestamp = new DateTimeOffset(2026, 6, 20, 11, 0, 0, TimeSpan.Zero),
                    Source = new EvidenceSource
                    {
                        EngineId = "naming-engine",
                        AtomId = "naming.prefix.validation",
                        RuleId = "STD-ARC-OPENINGS-V01",
                        CapabilityId = "naming.prefix.validation"
                    },
                    Category = EvidenceCategory.Validation,
                    Severity = EvidenceSeverity.Info,
                    Message = "Family name 'DR_HTL_Door_01' satisfies prefix rule."
                }
            ],
            Diagnostics =
            [
                new NamingEngineDiagnostic
                {
                    Code = "NamingEngine.Information",
                    Message = "Door prefix validation completed.",
                    Severity = NamingEngineDiagnosticSeverity.Information,
                    Location = "criteria:rules"
                }
            ],
            Statistics = new NamingValidationStatistics
            {
                ObjectsChecked = 1,
                ObjectsPassed = 1,
                ObjectsFailed = 0,
                RulesChecked = 1,
                FindingsCount = 1
            },
            Metadata = new Dictionary<string, string>
            {
                ["ruleId"] = "STD-ARC-OPENINGS-V01"
            }
        };
    }

    private static NormalizedCategory CreateDoorsCategory()
    {
        return new NormalizedCategory
        {
            Identifier = new NormalizedIdentifier { Id = "category-doors", Kind = "category" },
            Name = "Doors"
        };
    }

    private static NormalizedCategory CreateWindowsCategory()
    {
        return new NormalizedCategory
        {
            Identifier = new NormalizedIdentifier { Id = "category-windows", Kind = "category" },
            Name = "Windows"
        };
    }
}
