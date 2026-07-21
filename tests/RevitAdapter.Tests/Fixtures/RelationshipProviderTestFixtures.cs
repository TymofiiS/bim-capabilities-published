using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Tests.Mocks;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class RelationshipProviderTestFixtures
{
    internal const string CorrelationId = "corr-relationship-provider-001";

    internal static readonly DateTimeOffset FixedExecutedAt = new(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);

    internal static RevitRelationshipProvider CreateProvider()
    {
        return new RevitRelationshipProvider(
            new MockRevitRelationshipCatalog(CreateSampleCatalog()),
            new FixedFamilyQueryClock(FixedExecutedAt));
    }

    internal static IReadOnlyList<IRevitRelationshipCatalogEntry> CreateSampleCatalog()
    {
        return
        [
            CreateEntry(
                "family-001",
                "family",
                "nested-family-001",
                "family",
                NormalizedRelationshipType.Nested,
                RelationshipType.NestedFamily,
                "nestedFamily"),
            CreateEntry(
                "family-001",
                "family",
                "imported-cad-001",
                "importedCad",
                NormalizedRelationshipType.Reference,
                RelationshipType.ImportedCad,
                "importedCad"),
            CreateEntry(
                "family-001",
                "family",
                "hardware-family-001",
                "family",
                NormalizedRelationshipType.Reference,
                RelationshipType.Dependency,
                "familyDependency"),
            CreateEntry(
                "family-001",
                "family",
                "family-type-001",
                "familyType",
                NormalizedRelationshipType.TypeDefinition,
                RelationshipType.FamilyType,
                "familyType"),
            CreateEntry(
                "host-element-001",
                "element",
                "family-001",
                "family",
                NormalizedRelationshipType.Host,
                RelationshipType.Host,
                "host"),
            CreateEntry(
                "family-parent-001",
                "family",
                "family-child-001",
                "family",
                NormalizedRelationshipType.Parent,
                RelationshipType.ParentChild,
                "parentChild"),
            CreateEntry(
                "family-001",
                "family",
                "reference-family-001",
                "family",
                NormalizedRelationshipType.Reference,
                RelationshipType.Reference,
                "reference")
        ];
    }

    internal static RelationshipQuery CreateAllRelationshipsQuery()
    {
        return new RelationshipQuery
        {
            Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static RelationshipQuery CreateNestedFamilyQuery()
    {
        return new RelationshipQuery
        {
            SourceObjects = ["family-001"],
            RelationshipTypes = [RelationshipType.NestedFamily],
            Scope = new RelationshipQueryScope
            {
                Kind = RelationshipQueryScopeKind.SelectedFamilies,
                ScopeIdentifiers = ["family-001"]
            },
            CorrelationId = CorrelationId
        };
    }

    internal static RelationshipQuery CreateImportedCadQuery()
    {
        return new RelationshipQuery
        {
            SourceObjects = ["family-001"],
            RelationshipTypes = [RelationshipType.ImportedCad],
            Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static RelationshipQuery CreateHostQuery()
    {
        return new RelationshipQuery
        {
            TargetObjects = ["family-001"],
            RelationshipTypes = [RelationshipType.Host],
            Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static RelationshipQuery CreateDependencyQuery()
    {
        return new RelationshipQuery
        {
            SourceObjects = ["family-001"],
            RelationshipTypes = [RelationshipType.Dependency],
            Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static RelationshipQuery CreateParentChildQuery()
    {
        return new RelationshipQuery
        {
            RelationshipTypes = [RelationshipType.ParentChild],
            Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    private static RevitRelationshipCatalogEntry CreateEntry(
        string sourceId,
        string sourceKind,
        string targetId,
        string targetKind,
        NormalizedRelationshipType normalizedType,
        RelationshipType queryRelationshipType,
        string referenceType)
    {
        return RelationshipRetrievalSupport.CreateEntry(
            sourceId,
            sourceKind,
            targetId,
            targetKind,
            normalizedType,
            queryRelationshipType,
            referenceType);
    }
}
