using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Tests;

public class FamilyRetrievalTests
{
    private static readonly JsonSerializerOptions JsonOptions = FamilyRetrievalSerialization.Options;

    [Fact]
    public void Family_retrieval_contracts_are_data_only_types()
    {
        var retrievalTypes = typeof(FamilyQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(FamilyQuery).Namespace)
            .Where(type => type.Name.StartsWith("Family", StringComparison.Ordinal) || type.Name == nameof(IFamilyProvider));

        Assert.All(retrievalTypes, type =>
        {
            if (type == typeof(IFamilyProvider))
            {
                return;
            }

            if (type == typeof(FamilyQueryScopeKind) || type == typeof(FamilyQueryDiagnosticSeverity))
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
    public void FamilyQuery_can_be_constructed_with_required_properties()
    {
        var query = FamilyRetrievalTestData.CreateDoorFamilyQuery();

        Assert.Equal(["Doors"], query.Categories);
        Assert.Equal(["HTL_Door_01"], query.FamilyNames);
        Assert.Equal(["HTL_Door_01_900x2100"], query.FamilyTypeNames);
        Assert.Equal(FamilyQueryScopeKind.EntireModel, query.Scope!.Kind);
        Assert.Equal("STD-ARC-OPENINGS-V01", query.Metadata!["ruleId"]);
        Assert.Equal("corr-family-query-001", query.CorrelationId);
    }

    [Theory]
    [InlineData(FamilyQueryScopeKind.EntireModel)]
    [InlineData(FamilyQueryScopeKind.SelectedElements)]
    [InlineData(FamilyQueryScopeKind.SelectedFamilies)]
    [InlineData(FamilyQueryScopeKind.Custom)]
    public void FamilyQueryScope_supports_required_scope_kinds(FamilyQueryScopeKind scopeKind)
    {
        var scope = new FamilyQueryScope
        {
            Kind = scopeKind,
            ScopeIdentifiers = scopeKind == FamilyQueryScopeKind.Custom
                ? ["custom-scope-001"]
                : null
        };

        Assert.Equal(scopeKind, scope.Kind);
        if (scopeKind == FamilyQueryScopeKind.Custom)
        {
            Assert.Single(scope.ScopeIdentifiers!);
        }
    }

    [Fact]
    public void FamilyQueryFilter_supports_category_name_parameter_relationship_and_usage_filters()
    {
        var filter = FamilyRetrievalTestData.CreateDoorFamilyFilter();

        Assert.Equal(["Doors"], filter.Category!.CategoryNames);
        Assert.Equal("HTL_Door_*", filter.Name!.NamePattern);
        Assert.Equal("FireRating", filter.Parameter!.ParameterName);
        Assert.True(filter.Parameter.MustExist);
        Assert.Equal(NormalizedRelationshipType.Nested, filter.Relationship!.RelationshipType);
        Assert.True(filter.Usage!.IncludeNested);
        Assert.False(filter.Usage.IncludeInPlace);
    }

    [Fact]
    public void FamilyQueryResult_supports_families_diagnostics_statistics_and_metadata()
    {
        var result = FamilyRetrievalTestData.CreateDoorFamilyQueryResult();

        Assert.Single(result.Families);
        Assert.Equal("HTL_Door_01", result.Families[0].Name);
        Assert.Single(result.Diagnostics!);
        Assert.Equal("FamilyQuery.Information", result.Diagnostics![0].Code);
        Assert.Equal(10, result.Statistics!.TotalFamilies);
        Assert.Equal(1, result.Statistics.RetrievedFamilies);
        Assert.Equal("corr-family-query-001", result.QueryMetadata!.CorrelationId);
        Assert.Equal("revit-adapter-read-layer", result.QueryMetadata.ProviderId);
    }

    [Fact]
    public void FamilyQueryDiagnostic_supports_required_structure()
    {
        var diagnostic = new FamilyQueryDiagnostic
        {
            Code = "FamilyQuery.UnsupportedFilter",
            Message = "Relationship filter is not supported for the current scope.",
            Severity = FamilyQueryDiagnosticSeverity.Warning,
            Location = "filter:relationship",
            Data = new Dictionary<string, string>
            {
                ["scopeKind"] = "selectedElements"
            }
        };

        Assert.Equal(FamilyQueryDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("selectedElements", diagnostic.Data!["scopeKind"]);
    }

    [Fact]
    public void FamilyQuery_supports_json_round_trip_serialization()
    {
        var original = FamilyRetrievalTestData.CreateDoorFamilyQuery();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<FamilyQuery>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Categories, roundTrip.Categories);
        Assert.Equal(original.FamilyNames, roundTrip.FamilyNames);
        Assert.Equal(original.Scope!.Kind, roundTrip.Scope!.Kind);
        Assert.Equal(original.Filter!.Parameter!.ParameterName, roundTrip.Filter!.Parameter!.ParameterName);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void FamilyQueryResult_supports_json_round_trip_serialization()
    {
        var original = FamilyRetrievalTestData.CreateDoorFamilyQueryResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<FamilyQueryResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Families.Count, roundTrip.Families.Count);
        Assert.Equal(original.Families[0].Identity.Id, roundTrip.Families[0].Identity.Id);
        Assert.Equal(original.Diagnostics!.Count, roundTrip.Diagnostics!.Count);
        Assert.Equal(original.Statistics!.RetrievedFamilies, roundTrip.Statistics!.RetrievedFamilies);
        Assert.Equal(original.QueryMetadata!.ProviderId, roundTrip.QueryMetadata!.ProviderId);
    }

    [Fact]
    public void Family_retrieval_contracts_use_init_only_immutable_properties()
    {
        var contractTypes = typeof(FamilyQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(FamilyQuery).Namespace)
            .Where(type => type.IsClass)
            .Where(type => type.Name.StartsWith("Family", StringComparison.Ordinal));

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
    public void Family_retrieval_contracts_do_not_reference_revit_or_engine_assemblies()
    {
        var contractsAssembly = typeof(FamilyQuery).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Family", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void IFamilyProvider_defines_retrieval_contract_without_implementation()
    {
        var methods = typeof(IFamilyProvider).GetMethods();

        var retrieveMethod = Assert.Single(methods, method => method.Name == "Retrieve");
        Assert.Equal(typeof(FamilyQueryResult), retrieveMethod.ReturnType);
        Assert.Equal(typeof(FamilyQuery), retrieveMethod.GetParameters()[0].ParameterType);
    }
}
