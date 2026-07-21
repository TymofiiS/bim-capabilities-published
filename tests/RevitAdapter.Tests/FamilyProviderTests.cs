using System.Reflection;
using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Tests.Fixtures;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class FamilyProviderTests
{
    private readonly RevitFamilyProvider _provider =
        FamilyProviderTestFixtures.CreateProvider(FamilyProviderTestFixtures.CreateSampleCatalog());

    [Fact]
    public void Retrieve_all_families_returns_normalized_families()
    {
        var result = _provider.Retrieve(FamilyProviderTestFixtures.CreateAllFamiliesQuery());

        Assert.Equal(3, result.Families.Count);
        Assert.Equal(3, result.Statistics!.TotalFamilies);
        Assert.Equal(3, result.Statistics.RetrievedFamilies);
        Assert.Equal(0, result.Statistics.FilteredFamilies);
        Assert.Equal(FamilyProviderTestFixtures.CorrelationId, result.QueryMetadata!.CorrelationId);
        Assert.Equal(FamilyRetrievalSupport.ProviderId, result.QueryMetadata.ProviderId);
    }

    [Fact]
    public void Retrieve_door_families_returns_only_door_category()
    {
        var result = _provider.Retrieve(FamilyProviderTestFixtures.CreateDoorFamiliesQuery());

        Assert.Single(result.Families);
        Assert.Equal("HTL_Door_01", result.Families[0].Name);
        Assert.Equal("Doors", result.Families[0].Category!.Name);
        Assert.Equal(1, result.Statistics!.CountsByCategory!["Doors"]);
    }

    [Fact]
    public void Retrieve_window_families_returns_only_window_category()
    {
        var result = _provider.Retrieve(FamilyProviderTestFixtures.CreateWindowFamiliesQuery());

        Assert.Single(result.Families);
        Assert.Equal("HTL_Window_01", result.Families[0].Name);
        Assert.Equal("Windows", result.Families[0].Category!.Name);
        Assert.Equal(1, result.Statistics!.CountsByCategory!["Windows"]);
    }

    [Fact]
    public void Retrieve_family_by_name_returns_matching_family()
    {
        var result = _provider.Retrieve(FamilyProviderTestFixtures.CreateFamilyByNameQuery("HTL_Door_01"));

        Assert.Single(result.Families);
        Assert.Equal("family-door-001", result.Families[0].Identity.Id);
        Assert.Equal("family", result.Families[0].Identity.Kind);
    }

    [Fact]
    public void Retrieve_family_types_returns_matching_types_only()
    {
        var result = _provider.Retrieve(
            FamilyProviderTestFixtures.CreateFamilyTypesQuery("HTL_Door_01_900x2100"));

        Assert.Single(result.Families);
        Assert.Single(result.Families[0].FamilyTypes!);
        Assert.Equal("HTL_Door_01_900x2100", result.Families[0].FamilyTypes![0].Name);
        Assert.Equal("family-type-door-900", result.Families[0].FamilyTypes![0].Identity.Id);
    }

    [Fact]
    public void Empty_result_handling_emits_no_families_found_diagnostic()
    {
        var result = _provider.Retrieve(FamilyProviderTestFixtures.CreateFamilyByNameQuery("Missing_Family"));

        Assert.Empty(result.Families);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == FamilyRetrievalDiagnostics.NoFamiliesFound);
        Assert.Equal(0, result.Statistics!.RetrievedFamilies);
    }

    [Fact]
    public void Empty_catalog_emits_empty_result_diagnostic()
    {
        var provider = FamilyProviderTestFixtures.CreateProvider([]);

        var result = provider.Retrieve(FamilyProviderTestFixtures.CreateAllFamiliesQuery());

        Assert.Empty(result.Families);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == FamilyRetrievalDiagnostics.EmptyResult);
    }

    [Fact]
    public void Invalid_category_emits_error_diagnostic()
    {
        var query = new FamilyQuery
        {
            Categories = ["Plumbing"],
            Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
            CorrelationId = FamilyProviderTestFixtures.CorrelationId
        };

        var result = _provider.Retrieve(query);

        Assert.Empty(result.Families);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == FamilyRetrievalDiagnostics.InvalidCategory &&
            diagnostic.Severity == FamilyQueryDiagnosticSeverity.Error);
    }

    [Fact]
    public void Invalid_query_emits_error_diagnostic_for_missing_scope_identifiers()
    {
        var query = new FamilyQuery
        {
            Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.SelectedFamilies },
            CorrelationId = FamilyProviderTestFixtures.CorrelationId
        };

        var result = _provider.Retrieve(query);

        Assert.Empty(result.Families);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == FamilyRetrievalDiagnostics.InvalidQuery &&
            diagnostic.Severity == FamilyQueryDiagnosticSeverity.Error);
    }

    [Fact]
    public void Translation_correctness_populates_normalized_family_contracts()
    {
        var result = _provider.Retrieve(FamilyProviderTestFixtures.CreateDoorFamiliesQuery());
        var family = result.Families[0];

        Assert.Equal("family-door-001", family.Identity.Id);
        Assert.Equal("HTL_Door_01", family.Name);
        Assert.Equal("Doors", family.Category!.Name);
        Assert.Equal(2, family.FamilyTypes!.Count);
        Assert.Equal("HTL_Door_01_1000x2100", family.FamilyTypes[0].Name);
        Assert.Equal("HTL_Door_01_900x2100", family.FamilyTypes[1].Name);
        Assert.Equal("FireRating", family.Parameters![0].Name);
        Assert.Equal("60", family.Parameters[0].Value);
        Assert.True(family.Parameters[0].IsSharedParameter);
    }

    [Fact]
    public void Retrieve_output_is_deterministic_for_same_query()
    {
        var query = FamilyProviderTestFixtures.CreateAllFamiliesQuery();

        var first = _provider.Retrieve(query);
        var second = _provider.Retrieve(query);

        Assert.Equal(
            first.Families.Select(family => family.Identity.Id),
            second.Families.Select(family => family.Identity.Id));
        Assert.Equal(first.QueryMetadata!.ExecutedAt, second.QueryMetadata!.ExecutedAt);
        Assert.Equal(
            first.Diagnostics?.Select(diagnostic => diagnostic.Code),
            second.Diagnostics?.Select(diagnostic => diagnostic.Code));
    }

    [Fact]
    public void Retrieve_throws_when_query_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.Retrieve(null!));
    }
}

public class FamilyProviderArchitectureTests
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
    public void Family_provider_does_not_reference_forbidden_assemblies()
    {
        var referencedAssemblies = typeof(RevitFamilyProvider).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }
    }

    [Fact]
    public void Family_provider_does_not_contain_business_or_compliance_logic()
    {
        var providerTypes = typeof(RevitFamilyProvider).Assembly.GetTypes()
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
