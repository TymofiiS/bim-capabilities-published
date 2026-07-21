using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Tests;

internal static class ParameterEngineTestData
{
    internal const string DemoSharedParameterFilePath = @"D:\Demo\CompanySharedParameters.txt";

    internal static readonly string[] MvpDoorParameterNames =
    [
        "FireRating",
        "RoomName",
        "Manufacturer"
    ];

    internal static readonly string[] MvpWindowParameterNames =
    [
        "AcousticRating",
        "RoomName",
        "Manufacturer"
    ];

    internal static ParameterTargetSet CreateDoorTargetSet()
    {
        return new ParameterTargetSet
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
                    Name = "HTL_Door_01",
                    Category = CreateDoorsCategory()
                }
            ],
            TargetTypes =
            [
                new NormalizedFamilyType
                {
                    Identity = new NormalizedIdentifier { Id = "family-type-001", Kind = "familyType" },
                    Name = "HTL_Door_01_900x2100"
                }
            ],
            TargetParameters =
            [
                CreateParameter("FireRating", "60", isShared: true),
                CreateParameter("RoomName", "Lobby", isShared: false),
                CreateParameter("Manufacturer", "HTL Components", isShared: true)
            ],
            SelectionMetadata = new Dictionary<string, string>
            {
                ["scope"] = "all-door-families",
                ["sourceEngine"] = "family-engine"
            }
        };
    }

    internal static ParameterTargetSet CreateWindowTargetSet()
    {
        return new ParameterTargetSet
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
                    Name = "HTL_Window_01",
                    Category = CreateWindowsCategory()
                }
            ],
            TargetTypes =
            [
                new NormalizedFamilyType
                {
                    Identity = new NormalizedIdentifier { Id = "family-type-003", Kind = "familyType" },
                    Name = "HTL_Window_01_1200x1200"
                }
            ],
            TargetParameters =
            [
                CreateParameter("AcousticRating", "45", isShared: true),
                CreateParameter("RoomName", "Office", isShared: false),
                CreateParameter("Manufacturer", "HTL Components", isShared: true)
            ],
            SelectionMetadata = new Dictionary<string, string>
            {
                ["scope"] = "all-window-families",
                ["sourceEngine"] = "family-engine"
            }
        };
    }

    internal static ParameterValidationCriteria CreateDoorValidationCriteria()
    {
        return new ParameterValidationCriteria
        {
            Parameters =
            [
                CreateCriteriaDefinition("FireRating", sharedParameterRequired: true, valueRequired: true, categoryName: "Doors"),
                CreateCriteriaDefinition("RoomName", sharedParameterRequired: false, valueRequired: true, categoryName: "Doors"),
                CreateCriteriaDefinition("Manufacturer", sharedParameterRequired: true, valueRequired: true, categoryName: "Doors")
            ],
            SharedParameterFile = CreateSharedParameterFileReference(),
            Metadata = new Dictionary<string, string>
            {
                ["validationPurpose"] = "door-parameter-compliance"
            }
        };
    }

    internal static ParameterValidationCriteria CreateWindowValidationCriteria()
    {
        return new ParameterValidationCriteria
        {
            Parameters =
            [
                CreateCriteriaDefinition("AcousticRating", sharedParameterRequired: true, valueRequired: true, categoryName: "Windows"),
                CreateCriteriaDefinition("RoomName", sharedParameterRequired: false, valueRequired: true, categoryName: "Windows"),
                CreateCriteriaDefinition("Manufacturer", sharedParameterRequired: true, valueRequired: true, categoryName: "Windows")
            ],
            SharedParameterFile = CreateSharedParameterFileReference(),
            Metadata = new Dictionary<string, string>
            {
                ["validationPurpose"] = "window-parameter-compliance"
            }
        };
    }

    internal static ParameterValidationRequest CreateDoorValidationRequest()
    {
        return new ParameterValidationRequest
        {
            TargetSet = CreateDoorTargetSet(),
            Criteria = CreateDoorValidationCriteria(),
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-engine-001",
            Metadata = new Dictionary<string, string>
            {
                ["engineOperation"] = "validation"
            }
        };
    }

    internal static ParameterValidationResult CreateDoorValidationResult()
    {
        return new ParameterValidationResult
        {
            Findings =
            [
                new ParameterValidationFinding
                {
                    ParameterName = "FireRating",
                    Passed = true,
                    Message = "Required parameter 'FireRating' is present.",
                    TargetObjectId = "family-type-001",
                    TargetObjectKind = "familyType"
                }
            ],
            Evidence =
            [
                new EvidenceRecord
                {
                    EvidenceId = "evidence-parameter-001",
                    Timestamp = new DateTimeOffset(2026, 6, 20, 9, 0, 0, TimeSpan.Zero),
                    Source = new EvidenceSource
                    {
                        EngineId = "parameter-engine",
                        AtomId = "parameter.existence",
                        RuleId = "STD-ARC-OPENINGS-V01",
                        CapabilityId = "parameter.existence"
                    },
                    Category = EvidenceCategory.Validation,
                    Severity = EvidenceSeverity.Info,
                    Message = "Parameter 'FireRating' exists on target family type."
                }
            ],
            Diagnostics =
            [
                new ParameterEngineDiagnostic
                {
                    Code = "ParameterEngine.Information",
                    Message = "Door parameter validation completed.",
                    Severity = ParameterEngineDiagnosticSeverity.Information,
                    Location = "criteria:parameters"
                }
            ],
            Statistics = new ParameterValidationStatistics
            {
                ParametersChecked = 3,
                ParametersPassed = 3,
                ParametersFailed = 0,
                FindingsCount = 3
            },
            Metadata = new Dictionary<string, string>
            {
                ["ruleId"] = "STD-ARC-OPENINGS-V01"
            }
        };
    }

    internal static ParameterSharedParameterFileReference CreateSharedParameterFileReference()
    {
        return new ParameterSharedParameterFileReference
        {
            FilePath = DemoSharedParameterFilePath,
            FileVersion = "2026.1",
            Metadata = new Dictionary<string, string>
            {
                ["providedBy"] = "rule-configuration"
            }
        };
    }

    private static ParameterCriteriaDefinition CreateCriteriaDefinition(
        string parameterName,
        bool sharedParameterRequired,
        bool valueRequired,
        string categoryName)
    {
        return new ParameterCriteriaDefinition
        {
            ParameterName = parameterName,
            Required = true,
            SharedParameterRequired = sharedParameterRequired,
            ValueRequired = valueRequired,
            CategoryScope = new ParameterCategoryScopeCriteria
            {
                CategoryNames = [categoryName]
            },
            ObjectScope = new ParameterObjectScopeCriteria
            {
                Scope = ParameterEngineObjectScope.Type,
                FamilyTypeIdentifiers = categoryName == "Doors"
                    ? ["family-type-001"]
                    : ["family-type-003"]
            },
            Metadata = new Dictionary<string, string>
            {
                ["requirementSource"] = "bimrule"
            }
        };
    }

    private static NormalizedParameter CreateParameter(string name, string value, bool isShared)
    {
        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier
            {
                Id = $"parameter-{name.ToLowerInvariant()}",
                Kind = "parameter"
            },
            Name = name,
            Value = value,
            StorageType = NormalizedParameterStorageType.String,
            IsSharedParameter = isShared
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
