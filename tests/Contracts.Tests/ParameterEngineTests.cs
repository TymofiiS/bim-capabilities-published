using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Parameter;

namespace BIMCapabilities.Contracts.Tests;

public class ParameterEngineTests
{
    private static readonly JsonSerializerOptions JsonOptions = ParameterEngineSerialization.Options;

    [Fact]
    public void Parameter_engine_contracts_are_data_only_types()
    {
        var parameterEngineTypes = typeof(ParameterTargetSet).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ParameterTargetSet).Namespace);

        Assert.All(parameterEngineTypes, type =>
        {
            if (type == typeof(IParameterEngine))
            {
                return;
            }

            if (type == typeof(ParameterEngineDiagnosticSeverity) || type == typeof(ParameterEngineObjectScope))
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
    public void ParameterTargetSet_can_be_constructed_with_required_properties()
    {
        var targetSet = ParameterEngineTestData.CreateDoorTargetSet();

        Assert.Equal("target-set-doors-001", targetSet.TargetSetId);
        Assert.Single(targetSet.TargetFamilies!);
        Assert.Equal("HTL_Door_01", targetSet.TargetFamilies![0].Name);
        Assert.Single(targetSet.TargetTypes!);
        Assert.Equal(3, targetSet.TargetParameters!.Count);
        Assert.Equal("all-door-families", targetSet.SelectionMetadata!["scope"]);
    }

    [Fact]
    public void ParameterValidationCriteria_supports_name_required_shared_value_category_object_scope_and_metadata()
    {
        var criteria = ParameterEngineTestData.CreateDoorValidationCriteria();

        Assert.Equal(3, criteria.Parameters!.Count);
        Assert.Equal("FireRating", criteria.Parameters[0].ParameterName);
        Assert.True(criteria.Parameters[0].Required);
        Assert.True(criteria.Parameters[0].SharedParameterRequired);
        Assert.True(criteria.Parameters[0].ValueRequired);
        Assert.Equal(["Doors"], criteria.Parameters[0].CategoryScope!.CategoryNames);
        Assert.Equal(ParameterEngineObjectScope.Type, criteria.Parameters[0].ObjectScope!.Scope);
        Assert.Equal("door-parameter-compliance", criteria.Metadata!["validationPurpose"]);
    }

    [Fact]
    public void ParameterValidationCriteria_supports_mvp_door_and_window_parameters()
    {
        var doorCriteria = ParameterEngineTestData.CreateDoorValidationCriteria();
        var windowCriteria = ParameterEngineTestData.CreateWindowValidationCriteria();

        Assert.Equal(ParameterEngineTestData.MvpDoorParameterNames, doorCriteria.Parameters!.Select(definition => definition.ParameterName).ToArray());
        Assert.Equal(ParameterEngineTestData.MvpWindowParameterNames, windowCriteria.Parameters!.Select(definition => definition.ParameterName).ToArray());
    }

    [Fact]
    public void ParameterValidationCriteria_supports_shared_parameter_file_path()
    {
        var criteria = ParameterEngineTestData.CreateDoorValidationCriteria();

        Assert.Equal(ParameterEngineTestData.DemoSharedParameterFilePath, criteria.SharedParameterFile!.FilePath);
        Assert.Equal("2026.1", criteria.SharedParameterFile.FileVersion);
    }

    [Fact]
    public void ParameterValidationRequest_can_be_constructed_with_required_properties()
    {
        var request = ParameterEngineTestData.CreateDoorValidationRequest();

        Assert.Equal("STD-ARC-OPENINGS-V01", request.RuleId);
        Assert.Equal("corr-parameter-engine-001", request.CorrelationId);
        Assert.Equal("target-set-doors-001", request.TargetSet.TargetSetId);
        Assert.Equal(3, request.Criteria.Parameters!.Count);
    }

    [Fact]
    public void ParameterValidationResult_supports_findings_evidence_diagnostics_statistics_and_metadata()
    {
        var result = ParameterEngineTestData.CreateDoorValidationResult();

        Assert.Single(result.Findings!);
        Assert.Single(result.Evidence!);
        Assert.Single(result.Diagnostics!);
        Assert.Equal(3, result.Statistics!.ParametersPassed);
        Assert.Equal("STD-ARC-OPENINGS-V01", result.Metadata!["ruleId"]);
    }

    [Fact]
    public void ParameterEngineDiagnostic_supports_required_structure()
    {
        var diagnostic = new ParameterEngineDiagnostic
        {
            Code = "ParameterEngine.UnsupportedCriteria",
            Message = "Custom criteria key is not recognized.",
            Severity = ParameterEngineDiagnosticSeverity.Warning,
            Location = "criteria:parameters",
            Data = new Dictionary<string, string>
            {
                ["property"] = "unknownKey"
            }
        };

        Assert.Equal(ParameterEngineDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("unknownKey", diagnostic.Data!["property"]);
    }

    [Fact]
    public void ParameterValidationRequest_supports_json_round_trip_serialization()
    {
        var original = ParameterEngineTestData.CreateDoorValidationRequest();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ParameterValidationRequest>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RuleId, roundTrip.RuleId);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(original.Criteria.SharedParameterFile!.FilePath, roundTrip.Criteria.SharedParameterFile!.FilePath);
        Assert.Equal(original.Criteria.Parameters![0].ParameterName, roundTrip.Criteria.Parameters![0].ParameterName);
    }

    [Fact]
    public void ParameterValidationResult_supports_json_round_trip_serialization()
    {
        var original = ParameterEngineTestData.CreateDoorValidationResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ParameterValidationResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Findings!.Count, roundTrip.Findings!.Count);
        Assert.Equal(original.Statistics!.ParametersPassed, roundTrip.Statistics!.ParametersPassed);
        Assert.Equal(original.Diagnostics![0].Code, roundTrip.Diagnostics![0].Code);
    }

    [Fact]
    public void Parameter_engine_contracts_use_init_only_immutable_properties()
    {
        var contractTypes = typeof(ParameterTargetSet).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ParameterTargetSet).Namespace)
            .Where(type => type.IsClass);

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
    public void Parameter_engine_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ParameterTargetSet).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Parameter", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void IParameterEngine_defines_validation_contract_without_implementation()
    {
        var method = Assert.Single(typeof(IParameterEngine).GetMethods(), candidate => candidate.Name == "Validate");

        Assert.Equal(typeof(ParameterValidationResult), method.ReturnType);
        Assert.Equal(typeof(ParameterValidationRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Theory]
    [InlineData(ParameterEngineObjectScope.Instance)]
    [InlineData(ParameterEngineObjectScope.Type)]
    [InlineData(ParameterEngineObjectScope.Family)]
    [InlineData(ParameterEngineObjectScope.Category)]
    [InlineData(ParameterEngineObjectScope.Model)]
    public void ParameterEngineObjectScope_supports_required_scopes(ParameterEngineObjectScope scope)
    {
        var criteria = new ParameterObjectScopeCriteria
        {
            Scope = scope,
            ObjectIdentifiers = scope == ParameterEngineObjectScope.Model ? null : ["object-001"]
        };

        Assert.Equal(scope, criteria.Scope);
    }
}
