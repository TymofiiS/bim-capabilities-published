using System.Reflection;
using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Tests.Fixtures;
using BIMCapabilities.Adapters.Revit.Tests.Mocks;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class RelationshipProviderTests
{
    private readonly RevitRelationshipProvider _provider = RelationshipProviderTestFixtures.CreateProvider();

    [Fact]
    public void Retrieve_all_relationships_returns_normalized_relationships()
    {
        var result = _provider.Retrieve(RelationshipProviderTestFixtures.CreateAllRelationshipsQuery());

        Assert.Equal(7, result.Relationships.Count);
        Assert.Equal(7, result.Statistics!.RetrievedRelationships);
        Assert.Equal(RelationshipProviderTestFixtures.CorrelationId, result.QueryMetadata!.CorrelationId);
        Assert.Equal(RelationshipRetrievalSupport.ProviderId, result.QueryMetadata.ProviderId);
    }

    [Fact]
    public void Nested_family_retrieval_returns_nested_relationship()
    {
        var result = _provider.Retrieve(RelationshipProviderTestFixtures.CreateNestedFamilyQuery());

        var relationship = Assert.Single(result.Relationships);
        Assert.Equal("family-001", relationship.Source.Id);
        Assert.Equal("nested-family-001", relationship.Target.Id);
        Assert.Equal(NormalizedRelationshipType.Nested, relationship.RelationshipType);
        Assert.Equal(RelationshipType.NestedFamily.ToString(), relationship.Metadata!["queryRelationshipType"]);
        Assert.Equal("1", result.QueryMetadata!.Properties!["nestedFamilyRelationships"]);
    }

    [Fact]
    public void Imported_CAD_retrieval_returns_imported_cad_relationship()
    {
        var result = _provider.Retrieve(RelationshipProviderTestFixtures.CreateImportedCadQuery());

        var relationship = Assert.Single(result.Relationships);
        Assert.Equal("family-001", relationship.Source.Id);
        Assert.Equal("imported-cad-001", relationship.Target.Id);
        Assert.Equal("importedCad", relationship.Target.Kind);
        Assert.Equal(RelationshipType.ImportedCad.ToString(), relationship.Metadata!["queryRelationshipType"]);
        Assert.Equal("1", result.QueryMetadata!.Properties!["importedCadRelationships"]);
    }

    [Fact]
    public void Host_retrieval_returns_host_relationship()
    {
        var result = _provider.Retrieve(RelationshipProviderTestFixtures.CreateHostQuery());

        var relationship = Assert.Single(result.Relationships);
        Assert.Equal("host-element-001", relationship.Source.Id);
        Assert.Equal("family-001", relationship.Target.Id);
        Assert.Equal(NormalizedRelationshipType.Host, relationship.RelationshipType);
        Assert.Equal(RelationshipType.Host.ToString(), relationship.Metadata!["queryRelationshipType"]);
    }

    [Fact]
    public void Dependency_retrieval_returns_dependency_relationship()
    {
        var result = _provider.Retrieve(RelationshipProviderTestFixtures.CreateDependencyQuery());

        var relationship = Assert.Single(result.Relationships);
        Assert.Equal("family-001", relationship.Source.Id);
        Assert.Equal("hardware-family-001", relationship.Target.Id);
        Assert.Equal(RelationshipType.Dependency.ToString(), relationship.Metadata!["queryRelationshipType"]);
        Assert.Equal("familyDependency", relationship.Metadata["referenceType"]);
    }

    [Fact]
    public void ParentChild_retrieval_returns_parent_child_relationship()
    {
        var result = _provider.Retrieve(RelationshipProviderTestFixtures.CreateParentChildQuery());

        var relationship = Assert.Single(result.Relationships);
        Assert.Equal("family-parent-001", relationship.Source.Id);
        Assert.Equal("family-child-001", relationship.Target.Id);
        Assert.Equal(NormalizedRelationshipType.Parent, relationship.RelationshipType);
        Assert.Equal(RelationshipType.ParentChild.ToString(), relationship.Metadata!["queryRelationshipType"]);
    }

    [Fact]
    public void Empty_result_handling_emits_empty_result_diagnostic()
    {
        var emptyProvider = new RevitRelationshipProvider(
            new MockRevitRelationshipCatalog([]),
            new FixedFamilyQueryClock(RelationshipProviderTestFixtures.FixedExecutedAt));

        var result = emptyProvider.Retrieve(RelationshipProviderTestFixtures.CreateAllRelationshipsQuery());

        Assert.Empty(result.Relationships);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == RelationshipRetrievalDiagnostics.EmptyResult);
    }

    [Fact]
    public void Missing_relationship_type_emits_relationship_not_found_diagnostic()
    {
        var query = new RelationshipQuery
        {
            SourceObjects = ["family-001"],
            RelationshipTypes = [RelationshipType.FamilyType],
            Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.EntireModel },
            CorrelationId = RelationshipProviderTestFixtures.CorrelationId
        };

        var result = _provider.Retrieve(query with
        {
            TargetObjects = ["missing-target-001"]
        });

        Assert.Empty(result.Relationships);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == RelationshipRetrievalDiagnostics.RelationshipNotFound ||
            diagnostic.Code == RelationshipRetrievalDiagnostics.EmptyResult);
    }

    [Fact]
    public void Invalid_query_emits_error_diagnostic_for_missing_scope_identifiers()
    {
        var query = new RelationshipQuery
        {
            Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.SelectedFamilies },
            CorrelationId = RelationshipProviderTestFixtures.CorrelationId
        };

        var result = _provider.Retrieve(query);

        Assert.Empty(result.Relationships);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == RelationshipRetrievalDiagnostics.InvalidQuery &&
            diagnostic.Severity == RelationshipQueryDiagnosticSeverity.Error);
    }

    [Fact]
    public void Unsupported_relationship_type_emits_error_diagnostic()
    {
        var query = new RelationshipQuery
        {
            RelationshipTypes = [RelationshipType.Custom],
            Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.EntireModel },
            CorrelationId = RelationshipProviderTestFixtures.CorrelationId
        };

        var result = _provider.Retrieve(query);

        Assert.Empty(result.Relationships);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == RelationshipRetrievalDiagnostics.UnsupportedRelationship &&
            diagnostic.Severity == RelationshipQueryDiagnosticSeverity.Error);
    }

    [Fact]
    public void Translation_correctness_populates_normalized_relationship_contracts()
    {
        var result = _provider.Retrieve(RelationshipProviderTestFixtures.CreateNestedFamilyQuery());
        var relationship = Assert.Single(result.Relationships);

        Assert.Equal("family", relationship.Source.Kind);
        Assert.Equal("family", relationship.Target.Kind);
        Assert.Equal("nestedFamily", relationship.Metadata!["referenceType"]);
        Assert.Equal(1, result.Statistics!.CountsByRelationshipType![RelationshipType.NestedFamily.ToString()]);
    }

    [Fact]
    public void Retrieve_output_is_deterministic_for_same_query()
    {
        var query = RelationshipProviderTestFixtures.CreateAllRelationshipsQuery();

        var first = _provider.Retrieve(query);
        var second = _provider.Retrieve(query);

        Assert.Equal(
            first.Relationships.Select(relationship => $"{relationship.Source.Id}:{relationship.Target.Id}"),
            second.Relationships.Select(relationship => $"{relationship.Source.Id}:{relationship.Target.Id}"));
        Assert.Equal(first.QueryMetadata!.ExecutedAt, second.QueryMetadata!.ExecutedAt);
    }

    [Fact]
    public void Retrieve_throws_when_query_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.Retrieve(null!));
    }
}

public class RelationshipProviderArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyNames =
    [
        "BIMCapabilities.Runtime",
        "BIMCapabilities.Engines.Family",
        "BIMCapabilities.Engines.Parameter",
        "BIMCapabilities.Engines.Naming",
        "BIMCapabilities.Engines.Report",
        "BIMCapabilities.Launchers.Revit"
    ];

    [Fact]
    public void Relationship_provider_does_not_reference_forbidden_assemblies()
    {
        var referencedAssemblies = typeof(RevitRelationshipProvider).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }
    }

    [Fact]
    public void Relationship_provider_does_not_contain_business_or_compliance_logic()
    {
        var providerTypes = typeof(RevitRelationshipProvider).Assembly.GetTypes()
            .Where(type => type.Namespace is not null &&
                           type.Namespace.StartsWith("BIMCapabilities.Adapters.Revit.Read", StringComparison.Ordinal))
            .Where(type => type.IsClass && !type.IsAbstract && !type.Name.EndsWith("Skeleton", StringComparison.Ordinal));

        Assert.All(providerTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Validate", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Compliance", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("TargetSet", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }
}
