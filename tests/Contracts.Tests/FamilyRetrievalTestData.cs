using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Tests;

internal static class FamilyRetrievalTestData
{
    internal static FamilyQuery CreateDoorFamilyQuery()
    {
        return new FamilyQuery
        {
            Categories = ["Doors"],
            FamilyNames = ["HTL_Door_01"],
            FamilyTypeNames = ["HTL_Door_01_900x2100"],
            Scope = new FamilyQueryScope
            {
                Kind = FamilyQueryScopeKind.EntireModel,
                Metadata = new Dictionary<string, string>
                {
                    ["documentType"] = "project"
                }
            },
            Filter = CreateDoorFamilyFilter(),
            Metadata = new Dictionary<string, string>
            {
                ["ruleId"] = "STD-ARC-OPENINGS-V01",
                ["queryPurpose"] = "family-discovery"
            },
            CorrelationId = "corr-family-query-001"
        };
    }

    internal static FamilyQueryFilter CreateDoorFamilyFilter()
    {
        return new FamilyQueryFilter
        {
            Category = new FamilyCategoryFilter
            {
                CategoryNames = ["Doors"],
                CategoryIdentifiers = ["category-doors"]
            },
            Name = new FamilyNameFilter
            {
                ExactNames = ["HTL_Door_01"],
                NamePattern = "HTL_Door_*"
            },
            Parameter = new FamilyParameterFilter
            {
                ParameterName = "FireRating",
                MustExist = true
            },
            Relationship = new FamilyRelationshipFilter
            {
                RelationshipType = NormalizedRelationshipType.Nested,
                TargetKind = "family"
            },
            Usage = new FamilyUsageFilter
            {
                IncludeUnused = false,
                IncludeInPlace = false,
                IncludeNested = true
            }
        };
    }

    internal static FamilyQueryResult CreateDoorFamilyQueryResult()
    {
        return new FamilyQueryResult
        {
            Families = [RevitTranslationTestData.CreateDoorFamily()],
            Diagnostics =
            [
                new FamilyQueryDiagnostic
                {
                    Code = "FamilyQuery.Information",
                    Message = "Retrieved door families for requested scope.",
                    Severity = FamilyQueryDiagnosticSeverity.Information,
                    Location = "scope:entireModel"
                }
            ],
            Statistics = new FamilyQueryStatistics
            {
                TotalFamilies = 10,
                RetrievedFamilies = 1,
                FilteredFamilies = 9,
                CountsByCategory = new Dictionary<string, int>
                {
                    ["Doors"] = 1
                }
            },
            QueryMetadata = new FamilyQueryMetadata
            {
                CorrelationId = "corr-family-query-001",
                ExecutedAt = new DateTimeOffset(2026, 6, 19, 22, 0, 0, TimeSpan.Zero),
                ProviderId = "revit-adapter-read-layer",
                Properties = new Dictionary<string, string>
                {
                    ["scopeKind"] = "entireModel"
                }
            }
        };
    }
}
