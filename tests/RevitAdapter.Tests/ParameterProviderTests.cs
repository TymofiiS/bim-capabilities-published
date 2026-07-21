using System.Reflection;
using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Tests.Fixtures;
using BIMCapabilities.Adapters.Revit.Tests.Mocks;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class ParameterProviderTests
{
    private readonly RevitParameterProvider _provider = ParameterProviderTestFixtures.CreateProvider();

    [Fact]
    public void Retrieve_all_parameters_returns_normalized_parameters()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateAllParametersQuery());

        Assert.Equal(7, result.Parameters.Count);
        Assert.Equal(7, result.Statistics!.RetrievedParameters);
        Assert.Equal(ParameterProviderTestFixtures.CorrelationId, result.QueryMetadata!.CorrelationId);
        Assert.Equal(ParameterRetrievalSupport.ProviderId, result.QueryMetadata.ProviderId);
        Assert.Equal("4", result.QueryMetadata.Properties!["objectsInspected"]);
    }

    [Fact]
    public void Retrieve_RoomName_returns_normalized_parameter()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateParameterByNameQuery("RoomName"));

        Assert.Equal(2, result.Parameters.Count);
        Assert.All(result.Parameters, parameter => Assert.Equal("RoomName", parameter.Name));
        Assert.Contains(result.Parameters, parameter => parameter.Value == "Lobby" && !parameter.IsSharedParameter);
        Assert.Contains(result.Parameters, parameter => parameter.Value == "Office" && !parameter.IsSharedParameter);
    }

    [Fact]
    public void Retrieve_FireRating_returns_shared_parameter_with_value()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateParameterByNameQuery("FireRating"));

        var parameter = Assert.Single(result.Parameters);
        Assert.Equal("FireRating", parameter.Name);
        Assert.Equal("60", parameter.Value);
        Assert.True(parameter.IsSharedParameter);
        Assert.Equal(NormalizedParameterStorageType.String, parameter.StorageType);
    }

    [Fact]
    public void Retrieve_AcousticRating_returns_shared_parameters()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateParameterByNameQuery("AcousticRating"));

        Assert.Equal(2, result.Parameters.Count);
        Assert.All(result.Parameters, parameter =>
        {
            Assert.Equal("AcousticRating", parameter.Name);
            Assert.True(parameter.IsSharedParameter);
        });
    }

    [Fact]
    public void Retrieve_Manufacturer_returns_shared_parameters()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateParameterByNameQuery("Manufacturer"));

        Assert.Equal(2, result.Parameters.Count);
        Assert.All(result.Parameters, parameter =>
        {
            Assert.Equal("Manufacturer", parameter.Name);
            Assert.Equal("HTL Components", parameter.Value);
            Assert.True(parameter.IsSharedParameter);
        });
    }

    [Fact]
    public void Retrieve_shared_parameters_returns_only_shared_matches()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateSharedParametersQuery());

        Assert.Equal(5, result.Parameters.Count);
        Assert.All(result.Parameters, parameter => Assert.True(parameter.IsSharedParameter));
        Assert.Equal("5", result.QueryMetadata!.Properties!["sharedParametersRetrieved"]);
    }

    [Fact]
    public void Retrieve_by_category_returns_door_parameters_only()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateDoorCategoryQuery());

        Assert.Equal(4, result.Parameters.Count);
        Assert.All(result.Parameters, parameter =>
            Assert.Contains(result.Statistics!.CountsByParameterName!.Keys, name => name == parameter.Name));
        Assert.Equal(4, result.Statistics!.RetrievedParameters);
    }

    [Fact]
    public void Retrieve_by_family_returns_family_scoped_parameters()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateFamilyScopeQuery());

        Assert.Equal(4, result.Parameters.Count);
        Assert.All(result.Parameters, parameter =>
            Assert.StartsWith("parameter-", parameter.Identifier.Id, StringComparison.Ordinal));
        Assert.Contains(result.Parameters, parameter => parameter.Name == "FireRating");
    }

    [Fact]
    public void Empty_result_handling_emits_empty_result_diagnostic()
    {
        var emptyProvider = new RevitParameterProvider(
            new MockRevitParameterCatalog([]),
            new FixedFamilyQueryClock(ParameterProviderTestFixtures.FixedExecutedAt));

        var result = emptyProvider.Retrieve(ParameterProviderTestFixtures.CreateAllParametersQuery());

        Assert.Empty(result.Parameters);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == ParameterRetrievalDiagnostics.EmptyResult);
    }

    [Fact]
    public void Missing_parameter_emits_parameter_not_found_diagnostic()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateParameterByNameQuery("MissingParameter"));

        Assert.Empty(result.Parameters);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == ParameterRetrievalDiagnostics.ParameterNotFound);
        Assert.Equal(1, result.Statistics!.MissingParameters);
    }

    [Fact]
    public void Invalid_query_emits_error_diagnostic_for_missing_scope_identifiers()
    {
        var query = new ParameterQuery
        {
            Scope = new ParameterQueryScope { Kind = ParameterQueryScopeKind.SelectedFamilies },
            CorrelationId = ParameterProviderTestFixtures.CorrelationId
        };

        var result = _provider.Retrieve(query);

        Assert.Empty(result.Parameters);
        Assert.Contains(result.Diagnostics!, diagnostic =>
            diagnostic.Code == ParameterRetrievalDiagnostics.InvalidQuery &&
            diagnostic.Severity == ParameterQueryDiagnosticSeverity.Error);
    }

    [Fact]
    public void Translation_correctness_populates_normalized_parameter_contracts()
    {
        var result = _provider.Retrieve(ParameterProviderTestFixtures.CreateParameterByNameQuery("FireRating"));
        var parameter = Assert.Single(result.Parameters);

        Assert.Equal("parameter-fire-rating-door-900", parameter.Identifier.Id);
        Assert.Equal("parameter", parameter.Identifier.Kind);
        Assert.Equal("60", parameter.Value);
        Assert.Equal(ParameterProviderTestFixtures.FireRatingGuid, parameter.Metadata!["guid"]);
    }

    [Fact]
    public void Retrieve_output_is_deterministic_for_same_query()
    {
        var query = ParameterProviderTestFixtures.CreateAllParametersQuery();

        var first = _provider.Retrieve(query);
        var second = _provider.Retrieve(query);

        Assert.Equal(
            first.Parameters.Select(parameter => parameter.Identifier.Id),
            second.Parameters.Select(parameter => parameter.Identifier.Id));
        Assert.Equal(first.QueryMetadata!.ExecutedAt, second.QueryMetadata!.ExecutedAt);
    }

    [Fact]
    public void Retrieve_throws_when_query_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.Retrieve(null!));
    }
}

public class ParameterProviderArchitectureTests
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
    public void Parameter_provider_does_not_reference_forbidden_assemblies()
    {
        var referencedAssemblies = typeof(RevitParameterProvider).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }
    }

    [Fact]
    public void Parameter_provider_does_not_contain_business_or_compliance_logic()
    {
        var providerTypes = typeof(RevitParameterProvider).Assembly.GetTypes()
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
