using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Tests;

internal static class RelationshipRetrievalTestData
{
    internal static RelationshipQuery CreateNestedFamilyQuery()
    {
        return new RelationshipQuery
        {
            SourceObjects = ["family-001"],
            TargetObjects = ["nested-family-001"],
            RelationshipTypes = [RelationshipType.NestedFamily, RelationshipType.ParentChild],
            Scope = new RelationshipQueryScope
            {
                Kind = RelationshipQueryScopeKind.SelectedFamilies,
                ScopeIdentifiers = ["family-001"],
                Metadata = new Dictionary<string, string>
                {
                    ["documentType"] = "project"
                }
            },
            Filter = CreateNestedFamilyFilter(),
            Metadata = new Dictionary<string, string>
            {
                ["ruleId"] = "STD-ARC-OPENINGS-V01",
                ["queryPurpose"] = "nested-family-analysis"
            },
            CorrelationId = "corr-relationship-query-001"
        };
    }

    internal static RelationshipQueryFilter CreateNestedFamilyFilter()
    {
        return new RelationshipQueryFilter
        {
            RelationshipType = new RelationshipTypeFilter
            {
                RelationshipTypes = [RelationshipType.NestedFamily],
                IncludeCustom = false
            },
            Source = new RelationshipSourceFilter
            {
                SourceIdentifiers = ["family-001"],
                SourceKind = "family"
            },
            Target = new RelationshipTargetFilter
            {
                TargetIdentifiers = ["nested-family-001"],
                TargetKind = "family"
            },
            Category = new RelationshipCategoryFilter
            {
                CategoryNames = ["Doors"],
                CategoryIdentifiers = ["category-doors"]
            },
            Depth = new RelationshipDepthFilter
            {
                MaxDepth = 3,
                MinDepth = 1
            }
        };
    }

    internal static RelationshipQuery CreateImportedCadQuery()
    {
        return new RelationshipQuery
        {
            SourceObjects = ["family-001"],
            RelationshipTypes = [RelationshipType.ImportedCad, RelationshipType.Reference],
            Scope = new RelationshipQueryScope
            {
                Kind = RelationshipQueryScopeKind.EntireModel
            },
            Filter = new RelationshipQueryFilter
            {
                RelationshipType = new RelationshipTypeFilter
                {
                    RelationshipTypes = [RelationshipType.ImportedCad]
                },
                Target = new RelationshipTargetFilter
                {
                    TargetKind = "importedCad"
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["queryPurpose"] = "imported-cad-analysis"
            },
            CorrelationId = "corr-relationship-query-cad-001"
        };
    }

    internal static RelationshipQueryResult CreateMvpRelationshipQueryResult()
    {
        return new RelationshipQueryResult
        {
            Relationships =
            [
                CreateNormalizedRelationship(
                    "family-001",
                    "nested-family-001",
                    NormalizedRelationshipType.Nested,
                    RelationshipType.NestedFamily,
                    "nestedFamily"),
                CreateNormalizedRelationship(
                    "family-001",
                    "imported-cad-001",
                    NormalizedRelationshipType.Reference,
                    RelationshipType.ImportedCad,
                    "importedCad"),
                CreateNormalizedRelationship(
                    "family-001",
                    "hardware-family-001",
                    NormalizedRelationshipType.Reference,
                    RelationshipType.Dependency,
                    "familyDependency"),
                CreateNormalizedRelationship(
                    "family-001",
                    "family-type-001",
                    NormalizedRelationshipType.TypeDefinition,
                    RelationshipType.FamilyType,
                    "familyType")
            ],
            Diagnostics =
            [
                new RelationshipQueryDiagnostic
                {
                    Code = "RelationshipQuery.Information",
                    Message = "Retrieved MVP family relationships for selected scope.",
                    Severity = RelationshipQueryDiagnosticSeverity.Information,
                    Location = "scope:selectedFamilies"
                }
            ],
            Statistics = new RelationshipQueryStatistics
            {
                TotalRelationships = 6,
                RetrievedRelationships = 4,
                FilteredRelationships = 2,
                CountsByRelationshipType = new Dictionary<string, int>
                {
                    [RelationshipType.NestedFamily.ToString()] = 1,
                    [RelationshipType.ImportedCad.ToString()] = 1,
                    [RelationshipType.Dependency.ToString()] = 1,
                    [RelationshipType.FamilyType.ToString()] = 1
                }
            },
            QueryMetadata = new RelationshipQueryMetadata
            {
                CorrelationId = "corr-relationship-query-001",
                ExecutedAt = new DateTimeOffset(2026, 6, 19, 23, 0, 0, TimeSpan.Zero),
                ProviderId = "revit-adapter-read-layer",
                Properties = new Dictionary<string, string>
                {
                    ["scopeKind"] = "selectedFamilies"
                }
            }
        };
    }

    internal static NormalizedRelationship CreateNormalizedRelationship(
        string sourceId,
        string targetId,
        NormalizedRelationshipType normalizedType,
        RelationshipType queryType,
        string referenceType)
    {
        return new NormalizedRelationship
        {
            Source = new NormalizedIdentifier
            {
                Id = sourceId,
                Kind = "family"
            },
            Target = new NormalizedIdentifier
            {
                Id = targetId,
                Kind = queryType == RelationshipType.ImportedCad ? "importedCad" : "family"
            },
            RelationshipType = normalizedType,
            Metadata = new Dictionary<string, string>
            {
                ["queryRelationshipType"] = queryType.ToString(),
                ["referenceType"] = referenceType
            }
        };
    }
}
