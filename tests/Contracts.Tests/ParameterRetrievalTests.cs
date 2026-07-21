using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Contracts.Tests;

public class ParameterRetrievalTests
{
    private static readonly JsonSerializerOptions JsonOptions = ParameterRetrievalSerialization.Options;

    [Fact]
    public void Parameter_retrieval_contracts_are_data_only_types()
    {
        var retrievalTypes = typeof(ParameterQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ParameterQuery).Namespace)
            .Where(type => type.Name.StartsWith("Parameter", StringComparison.Ordinal) || type.Name == nameof(IParameterProvider));

        Assert.All(retrievalTypes, type =>
        {
            if (type == typeof(IParameterProvider))
            {
                return;
            }

            if (type == typeof(ParameterQueryScopeKind) || type == typeof(ParameterQueryDiagnosticSeverity))
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
    public void ParameterQuery_can_be_constructed_with_required_properties()
    {
        var query = ParameterRetrievalTestData.CreateMvpDoorParameterQuery();

        Assert.Equal(ParameterRetrievalTestData.MvpDoorParameterNames, query.ParameterNames);
        Assert.Contains("FireRating", query.SharedParameterNames!);
        Assert.Contains("ROOM_NAME", query.BuiltInParameterNames!);
        Assert.Equal(["Doors"], query.Categories);
        Assert.Equal(ParameterQueryScopeKind.SelectedFamilies, query.Scope!.Kind);
        Assert.Equal("family-type-001", query.ObjectScope!.FamilyTypeIdentifiers![0]);
        Assert.Equal(ParameterRetrievalTestData.SharedParameterFilePath, query.SharedParameterFile!.FilePath);
        Assert.Equal("STD-ARC-OPENINGS-V01", query.Metadata!["ruleId"]);
    }

    [Theory]
    [InlineData(ParameterQueryScopeKind.EntireModel)]
    [InlineData(ParameterQueryScopeKind.SelectedElements)]
    [InlineData(ParameterQueryScopeKind.SelectedFamilies)]
    [InlineData(ParameterQueryScopeKind.SelectedFamilyTypes)]
    [InlineData(ParameterQueryScopeKind.Custom)]
    public void ParameterQueryScope_supports_required_scope_kinds(ParameterQueryScopeKind scopeKind)
    {
        var scope = new ParameterQueryScope
        {
            Kind = scopeKind,
            ScopeIdentifiers = scopeKind == ParameterQueryScopeKind.Custom
                ? ["custom-parameter-scope-001"]
                : null
        };

        Assert.Equal(scopeKind, scope.Kind);
        if (scopeKind == ParameterQueryScopeKind.Custom)
        {
            Assert.Single(scope.ScopeIdentifiers!);
        }
    }

    [Fact]
    public void ParameterQueryFilter_supports_name_shared_value_category_and_object_filters()
    {
        var filter = ParameterRetrievalTestData.CreateMvpDoorParameterFilter();

        Assert.Equal(["FireRating", "Manufacturer"], filter.ParameterName!.ExactNames);
        Assert.Equal(["FireRating", "AcousticRating"], filter.SharedParameter!.SharedParameterNames);
        Assert.True(filter.SharedParameter.MustExist);
        Assert.True(filter.Value!.MustHaveValue);
        Assert.Equal(["Doors"], filter.Category!.CategoryNames);
        Assert.Equal("familyType", filter.Object!.ObjectKind);
    }

    [Fact]
    public void ParameterQueryResult_supports_parameters_diagnostics_statistics_and_metadata()
    {
        var result = ParameterRetrievalTestData.CreateMvpDoorParameterQueryResult();

        Assert.Equal(4, result.Parameters.Count);
        Assert.Single(result.Diagnostics!);
        Assert.Equal(4, result.Statistics!.RetrievedParameters);
        Assert.Equal("corr-parameter-query-001", result.QueryMetadata!.CorrelationId);
        Assert.Equal(ParameterRetrievalTestData.SharedParameterFilePath, result.QueryMetadata.Properties!["sharedParameterFile"]);
    }

    [Fact]
    public void ParameterQueryDiagnostic_supports_required_structure()
    {
        var diagnostic = new ParameterQueryDiagnostic
        {
            Code = "ParameterQuery.MissingSharedParameter",
            Message = "Shared parameter 'FireRating' was not found in the referenced file.",
            Severity = ParameterQueryDiagnosticSeverity.Warning,
            Location = "sharedParameter:FireRating",
            Data = new Dictionary<string, string>
            {
                ["sharedParameterFile"] = ParameterRetrievalTestData.SharedParameterFilePath
            }
        };

        Assert.Equal(ParameterQueryDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(ParameterRetrievalTestData.SharedParameterFilePath, diagnostic.Data!["sharedParameterFile"]);
    }

    [Fact]
    public void Mvp_door_parameter_scenarios_are_represented()
    {
        var query = ParameterRetrievalTestData.CreateMvpDoorParameterQuery();
        var result = ParameterRetrievalTestData.CreateMvpDoorParameterQueryResult();

        foreach (var parameterName in ParameterRetrievalTestData.MvpDoorParameterNames)
        {
            Assert.Contains(parameterName, query.ParameterNames!);
            Assert.Contains(result.Parameters, parameter => parameter.Name == parameterName);
        }
    }

    [Fact]
    public void Mvp_window_parameter_scenarios_are_represented()
    {
        var query = new ParameterQuery
        {
            ParameterNames = ParameterRetrievalTestData.MvpWindowParameterNames,
            Categories = ["Windows"],
            CorrelationId = "corr-parameter-query-window-001"
        };

        Assert.Equal(["AcousticRating", "RoomName", "Manufacturer"], query.ParameterNames);
        Assert.Equal(["Windows"], query.Categories);
    }

    [Fact]
    public void Shared_parameter_file_integration_is_represented()
    {
        var reference = ParameterRetrievalTestData.CreateSharedParameterFileReference();

        Assert.Equal(ParameterRetrievalTestData.SharedParameterFilePath, reference.FilePath);
        Assert.Equal("2026.1", reference.FileVersion);
        Assert.Equal("office-standards", reference.Metadata!["owner"]);
    }

    [Fact]
    public void ParameterQuery_supports_json_round_trip_serialization()
    {
        var original = ParameterRetrievalTestData.CreateMvpDoorParameterQuery();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ParameterQuery>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.ParameterNames, roundTrip.ParameterNames);
        Assert.Equal(original.SharedParameterGuids, roundTrip.SharedParameterGuids);
        Assert.Equal(original.Scope!.Kind, roundTrip.Scope!.Kind);
        Assert.Equal(original.Filter!.SharedParameter!.SharedParameterFilePath, roundTrip.Filter!.SharedParameter!.SharedParameterFilePath);
        Assert.Equal(original.SharedParameterFile!.FilePath, roundTrip.SharedParameterFile!.FilePath);
    }

    [Fact]
    public void ParameterQueryResult_supports_json_round_trip_serialization()
    {
        var original = ParameterRetrievalTestData.CreateMvpDoorParameterQueryResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ParameterQueryResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Parameters.Count, roundTrip.Parameters.Count);
        Assert.Equal(original.Parameters[0].Name, roundTrip.Parameters[0].Name);
        Assert.True(roundTrip.Parameters[0].IsSharedParameter);
        Assert.Equal(original.Diagnostics!.Count, roundTrip.Diagnostics!.Count);
        Assert.Equal(original.Statistics!.MissingParameters, roundTrip.Statistics!.MissingParameters);
    }

    [Fact]
    public void Parameter_retrieval_contracts_use_init_only_immutable_properties()
    {
        var contractTypes = typeof(ParameterQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ParameterQuery).Namespace)
            .Where(type => type.IsClass)
            .Where(type => type.Name.StartsWith("Parameter", StringComparison.Ordinal));

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
    public void Parameter_retrieval_contracts_do_not_reference_revit_or_engine_assemblies()
    {
        var contractsAssembly = typeof(ParameterQuery).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Parameter", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void IParameterProvider_defines_retrieval_contract_without_implementation()
    {
        var methods = typeof(IParameterProvider).GetMethods();

        var retrieveMethod = Assert.Single(methods, method => method.Name == "Retrieve");
        Assert.Equal(typeof(ParameterQueryResult), retrieveMethod.ReturnType);
        Assert.Equal(typeof(ParameterQuery), retrieveMethod.GetParameters()[0].ParameterType);
    }
}
