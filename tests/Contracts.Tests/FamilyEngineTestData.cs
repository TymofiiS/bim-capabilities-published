using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;

namespace BIMCapabilities.Contracts.Tests;

internal static class FamilyEngineTestData
{
    internal static FamilyTargetSet CreateDoorTargetSet()
    {
        return new FamilyTargetSet
        {
            TargetSetId = "target-set-doors-001",
            Families = [CreateDoorFamily()],
            FamilyTypes =
            [
                new NormalizedFamilyType
                {
                    Identity = new NormalizedIdentifier { Id = "family-type-001", Kind = "familyType" },
                    Name = "HTL_Door_01_900x2100"
                }
            ],
            Categories = [CreateDoorsCategory()],
            Relationships =
            [
                new NormalizedRelationship
                {
                    Source = new NormalizedIdentifier { Id = "family-001", Kind = "family" },
                    Target = new NormalizedIdentifier { Id = "nested-family-001", Kind = "family" },
                    RelationshipType = NormalizedRelationshipType.Nested
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["scope"] = "selectedFamilies"
            }
        };
    }

    internal static FamilySelectionCriteria CreateDoorSelectionCriteria()
    {
        return new FamilySelectionCriteria
        {
            Categories = new FamilyCategoryCriteria
            {
                CategoryNames = ["Doors"],
                CategoryIdentifiers = ["category-doors"]
            },
            Names = new FamilyNameCriteria
            {
                ExactNames = ["HTL_Door_01"],
                NamePattern = "HTL_Door_*"
            },
            Parameters = new FamilyParameterCriteria
            {
                ParameterNames = ["FireRating"],
                MustExist = true
            },
            Relationships = new FamilyRelationshipCriteria
            {
                RelationshipTypes = [NormalizedRelationshipType.Nested],
                TargetKind = "family"
            },
            Usage = new FamilyUsageCriteria
            {
                IncludeNested = true,
                IncludeInPlace = false,
                IncludeUnused = false
            },
            Custom = new FamilyCustomCriteria
            {
                Properties = new Dictionary<string, string>
                {
                    ["selectionPurpose"] = "openings-validation"
                }
            }
        };
    }

    internal static FamilySelectionRequest CreateDoorSelectionRequest()
    {
        return new FamilySelectionRequest
        {
            Criteria = CreateDoorSelectionCriteria(),
            SourceTargetSet = CreateDoorTargetSet(),
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-engine-001",
            Metadata = new Dictionary<string, string>
            {
                ["engineOperation"] = "selection"
            }
        };
    }

    internal static FamilySelectionResult CreateDoorSelectionResult()
    {
        return new FamilySelectionResult
        {
            SelectedFamilies = CreateDoorTargetSet() with { TargetSetId = "selected-doors-001" },
            Diagnostics =
            [
                new FamilyEngineDiagnostic
                {
                    Code = "FamilyEngine.Information",
                    Message = "Door families selected for validation scope.",
                    Severity = FamilyEngineDiagnosticSeverity.Information,
                    Location = "criteria:categories"
                }
            ],
            Statistics = new FamilySelectionStatistics
            {
                CandidateFamilies = 10,
                SelectedFamilies = 1,
                FilteredFamilies = 9,
                CountsByCategory = new Dictionary<string, int> { ["Doors"] = 1 }
            },
            Metadata = new Dictionary<string, string>
            {
                ["ruleId"] = "STD-ARC-OPENINGS-V01"
            }
        };
    }

    private static NormalizedFamily CreateDoorFamily()
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier
            {
                Id = "family-001",
                Kind = "family",
                Scope = "project-document"
            },
            Name = "HTL_Door_01",
            Category = CreateDoorsCategory()
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
}
