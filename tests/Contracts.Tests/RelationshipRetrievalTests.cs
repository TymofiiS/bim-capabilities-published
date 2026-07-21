using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Tests;

public class RelationshipRetrievalTests
{
    private static readonly JsonSerializerOptions JsonOptions = RelationshipRetrievalSerialization.Options;

    [Fact]
    public void Relationship_retrieval_contracts_are_data_only_types()
    {
        var retrievalTypes = typeof(RelationshipQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(RelationshipQuery).Namespace)
            .Where(type => type.Name.StartsWith("Relationship", StringComparison.Ordinal) || type.Name == nameof(IRelationshipProvider));

        Assert.All(retrievalTypes, type =>
        {
            if (type == typeof(IRelationshipProvider))
            {
                return;
            }

            if (type == typeof(RelationshipType)
                || type == typeof(RelationshipQueryScopeKind)
                || type == typeof(RelationshipQueryDiagnosticSeverity))
            {
                return;
            }

            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void RelationshipQuery_can_be_constructed_with_required_properties()
    {
        var query = RelationshipRetrievalTestData.CreateNestedFamilyQuery();

        Assert.Equal(["family-001"], query.SourceObjects);
        Assert.Equal(["nested-family-001"], query.TargetObjects);
        Assert.Contains(RelationshipType.NestedFamily, query.RelationshipTypes!);
        Assert.Equal(RelationshipQueryScopeKind.SelectedFamilies, query.Scope!.Kind);
        Assert.Equal("STD-ARC-OPENINGS-V01", query.Metadata!["ruleId"]);
        Assert.Equal("corr-relationship-query-001", query.CorrelationId);
    }

    [Theory]
    [InlineData(RelationshipType.ParentChild)]
    [InlineData(RelationshipType.NestedFamily)]
    [InlineData(RelationshipType.ImportedCad)]
    [InlineData(RelationshipType.FamilyType)]
    [InlineData(RelationshipType.Host)]
    [InlineData(RelationshipType.Dependency)]
    [InlineData(RelationshipType.Reference)]
    [InlineData(RelationshipType.Custom)]
    public void RelationshipType_supports_required_values(RelationshipType relationshipType)
    {
        var query = new RelationshipQuery
        {
            RelationshipTypes = [relationshipType],
            CorrelationId = "relationship-type-test"
        };

        Assert.Equal(relationshipType, query.RelationshipTypes![0]);
    }

    [Theory]
    [InlineData(RelationshipQueryScopeKind.EntireModel)]
    [InlineData(RelationshipQueryScopeKind.SelectedElements)]
    [InlineData(RelationshipQueryScopeKind.SelectedFamilies)]
    [InlineData(RelationshipQueryScopeKind.SelectedFamilyTypes)]
    [InlineData(RelationshipQueryScopeKind.Custom)]
    public void RelationshipQueryScope_supports_required_scope_kinds(RelationshipQueryScopeKind scopeKind)
    {
        var scope = new RelationshipQueryScope
        {
            Kind = scopeKind,
            ScopeIdentifiers = scopeKind == RelationshipQueryScopeKind.Custom
                ? ["custom-relationship-scope-001"]
                : null
        };

        Assert.Equal(scopeKind, scope.Kind);
        if (scopeKind == RelationshipQueryScopeKind.Custom)
        {
            Assert.Single(scope.ScopeIdentifiers!);
        }
    }

    [Fact]
    public void RelationshipQueryFilter_supports_type_source_target_category_and_depth_filters()
    {
        var filter = RelationshipRetrievalTestData.CreateNestedFamilyFilter();

        Assert.Equal([RelationshipType.NestedFamily], filter.RelationshipType!.RelationshipTypes);
        Assert.Equal("family", filter.Source!.SourceKind);
        Assert.Equal("family", filter.Target!.TargetKind);
        Assert.Equal(["Doors"], filter.Category!.CategoryNames);
        Assert.Equal(3, filter.Depth!.MaxDepth);
    }

    [Fact]
    public void RelationshipQueryResult_supports_relationships_diagnostics_statistics_and_metadata()
    {
        var result = RelationshipRetrievalTestData.CreateMvpRelationshipQueryResult();

        Assert.Equal(4, result.Relationships.Count);
        Assert.Single(result.Diagnostics!);
        Assert.Equal(4, result.Statistics!.RetrievedRelationships);
        Assert.Equal("corr-relationship-query-001", result.QueryMetadata!.CorrelationId);
    }

    [Fact]
    public void Nested_family_support_is_represented()
    {
        var query = RelationshipRetrievalTestData.CreateNestedFamilyQuery();
        var result = RelationshipRetrievalTestData.CreateMvpRelationshipQueryResult();

        Assert.Contains(RelationshipType.NestedFamily, query.RelationshipTypes!);
        Assert.Contains(result.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.NestedFamily.ToString());
    }

    [Fact]
    public void Imported_cad_support_is_represented()
    {
        var query = RelationshipRetrievalTestData.CreateImportedCadQuery();
        var result = RelationshipRetrievalTestData.CreateMvpRelationshipQueryResult();

        Assert.Contains(RelationshipType.ImportedCad, query.RelationshipTypes!);
        Assert.Equal("importedCad", query.Filter!.Target!.TargetKind);
        Assert.Contains(result.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.ImportedCad.ToString());
    }

    [Fact]
    public void Family_dependency_and_relationship_scenarios_are_represented()
    {
        var result = RelationshipRetrievalTestData.CreateMvpRelationshipQueryResult();

        Assert.Contains(result.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.Dependency.ToString());
        Assert.Contains(result.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.FamilyType.ToString());
        Assert.Equal(1, result.Statistics!.CountsByRelationshipType![RelationshipType.Dependency.ToString()]);
    }

    [Fact]
    public void RelationshipQueryDiagnostic_supports_required_structure()
    {
        var diagnostic = new RelationshipQueryDiagnostic
        {
            Code = "RelationshipQuery.UnsupportedDepth",
            Message = "Requested relationship depth exceeds adapter capability.",
            Severity = RelationshipQueryDiagnosticSeverity.Warning,
            Location = "filter:depth",
            Data = new Dictionary<string, string>
            {
                ["maxDepth"] = "3"
            }
        };

        Assert.Equal(RelationshipQueryDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("3", diagnostic.Data!["maxDepth"]);
    }

    [Fact]
    public void RelationshipQuery_supports_json_round_trip_serialization()
    {
        var original = RelationshipRetrievalTestData.CreateNestedFamilyQuery();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<RelationshipQuery>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.SourceObjects, roundTrip.SourceObjects);
        Assert.Equal(original.RelationshipTypes, roundTrip.RelationshipTypes);
        Assert.Equal(original.Scope!.Kind, roundTrip.Scope!.Kind);
        Assert.Equal(original.Filter!.Depth!.MaxDepth, roundTrip.Filter!.Depth!.MaxDepth);
    }

    [Fact]
    public void RelationshipQueryResult_supports_json_round_trip_serialization()
    {
        var original = RelationshipRetrievalTestData.CreateMvpRelationshipQueryResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<RelationshipQueryResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Relationships.Count, roundTrip.Relationships.Count);
        Assert.Equal(original.Relationships[0].Source.Id, roundTrip.Relationships[0].Source.Id);
        Assert.Equal(original.Relationships[0].RelationshipType, roundTrip.Relationships[0].RelationshipType);
        Assert.Equal(original.Statistics!.FilteredRelationships, roundTrip.Statistics!.FilteredRelationships);
    }

    [Fact]
    public void Relationship_retrieval_contracts_use_init_only_immutable_properties()
    {
        var contractTypes = typeof(RelationshipQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(RelationshipQuery).Namespace)
            .Where(type => type.IsClass)
            .Where(type => type.Name.StartsWith("Relationship", StringComparison.Ordinal));

        Assert.All(contractTypes, type =>
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.NotNull(property.SetMethod);
                Assert.Contains(
                    property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers(),
                    modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
            }
        });
    }

    [Fact]
    public void Relationship_retrieval_contracts_do_not_reference_revit_or_engine_assemblies()
    {
        var contractsAssembly = typeof(RelationshipQuery).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Family", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void IRelationshipProvider_defines_retrieval_contract_without_implementation()
    {
        var methods = typeof(IRelationshipProvider).GetMethods();

        var retrieveMethod = Assert.Single(methods, method => method.Name == "Retrieve");
        Assert.Equal(typeof(RelationshipQueryResult), retrieveMethod.ReturnType);
        Assert.Equal(typeof(RelationshipQuery), retrieveMethod.GetParameters()[0].ParameterType);
    }
}
