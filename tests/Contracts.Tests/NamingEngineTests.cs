using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Naming;

namespace BIMCapabilities.Contracts.Tests;

public class NamingEngineTests
{
    private static readonly JsonSerializerOptions JsonOptions = NamingEngineSerialization.Options;

    [Fact]
    public void Naming_engine_contracts_are_data_only_types()
    {
        var namingEngineTypes = typeof(NamingTargetSet).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(NamingTargetSet).Namespace);

        Assert.All(namingEngineTypes, type =>
        {
            if (type == typeof(INamingEngine))
            {
                return;
            }

            if (type == typeof(NamingEngineDiagnosticSeverity)
                || type == typeof(NamingEngineObjectScope)
                || type == typeof(NamingCaseRule))
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
    public void NamingTargetSet_can_be_constructed_with_required_properties()
    {
        var targetSet = NamingEngineTestData.CreateDoorTargetSet();

        Assert.Equal("target-set-doors-001", targetSet.TargetSetId);
        Assert.Single(targetSet.TargetFamilies!);
        Assert.Equal("DR_HTL_Door_01", targetSet.TargetFamilies![0].Name);
        Assert.Single(targetSet.TargetTypes!);
        Assert.Single(targetSet.Categories!);
        Assert.Equal("all-door-families", targetSet.SelectionMetadata!["scope"]);
    }

    [Fact]
    public void NamingValidationCriteria_supports_prefix_suffix_pattern_regex_case_and_custom_rule()
    {
        var criteria = NamingEngineTestData.CreatePatternCriteria();

        Assert.Equal("{Prefix}_{Discipline}_{Element}_{Variant}", criteria.NamingPattern);
        Assert.Equal(@"^[A-Z]{2}_[A-Z0-9_]+$", criteria.RegularExpression);
        Assert.Equal(NamingCaseRule.PascalCase, criteria.CaseRule);
        Assert.Equal("client.openings.naming-pattern", criteria.CustomRuleIdentifier);
        Assert.Equal("{Prefix}_{Discipline}_{Element}_{Variant}", criteria.Rules![0].NamingPattern);
    }

    [Fact]
    public void NamingValidationCriteria_supports_mvp_door_prefix_scenario()
    {
        var criteria = NamingEngineTestData.CreateDoorPrefixCriteria();

        Assert.Equal(NamingEngineTestData.DoorPrefix, criteria.RequiredPrefix);
        Assert.Equal(NamingEngineTestData.DoorPrefix, criteria.Rules![0].RequiredPrefix);
        Assert.Equal(["Doors"], criteria.Rules[0].CategoryScope!.CategoryNames);
        Assert.Equal(NamingEngineObjectScope.Family, criteria.Rules[0].ObjectScope!.Scope);
    }

    [Fact]
    public void NamingValidationCriteria_supports_mvp_window_prefix_scenario()
    {
        var criteria = NamingEngineTestData.CreateWindowPrefixCriteria();

        Assert.Equal(NamingEngineTestData.WindowPrefix, criteria.RequiredPrefix);
        Assert.Equal(NamingEngineTestData.WindowPrefix, criteria.Rules![0].RequiredPrefix);
        Assert.Equal(["Windows"], criteria.Rules[0].CategoryScope!.CategoryNames);
    }

    [Fact]
    public void NamingValidationRequest_can_be_constructed_with_required_properties()
    {
        var request = NamingEngineTestData.CreateDoorValidationRequest();

        Assert.Equal("STD-ARC-OPENINGS-V01", request.RuleId);
        Assert.Equal("corr-naming-engine-001", request.CorrelationId);
        Assert.Equal("target-set-doors-001", request.TargetSet.TargetSetId);
        Assert.Equal(NamingEngineTestData.DoorPrefix, request.Criteria.RequiredPrefix);
    }

    [Fact]
    public void NamingValidationResult_supports_findings_evidence_diagnostics_statistics_and_metadata()
    {
        var result = NamingEngineTestData.CreateDoorValidationResult();

        Assert.Single(result.Findings!);
        Assert.Single(result.Evidence!);
        Assert.Single(result.Diagnostics!);
        Assert.Equal(1, result.Statistics!.ObjectsPassed);
        Assert.Equal("STD-ARC-OPENINGS-V01", result.Metadata!["ruleId"]);
    }

    [Fact]
    public void NamingEngineDiagnostic_supports_required_structure()
    {
        var diagnostic = new NamingEngineDiagnostic
        {
            Code = "NamingEngine.UnsupportedCriteria",
            Message = "Custom naming rule is not recognized.",
            Severity = NamingEngineDiagnosticSeverity.Warning,
            Location = "criteria:rules",
            Data = new Dictionary<string, string>
            {
                ["property"] = "unknownKey"
            }
        };

        Assert.Equal(NamingEngineDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("unknownKey", diagnostic.Data!["property"]);
    }

    [Fact]
    public void NamingValidationRequest_supports_json_round_trip_serialization()
    {
        var original = NamingEngineTestData.CreateDoorValidationRequest();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<NamingValidationRequest>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RuleId, roundTrip.RuleId);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(original.Criteria.RequiredPrefix, roundTrip.Criteria.RequiredPrefix);
        Assert.Equal(original.TargetSet.TargetSetId, roundTrip.TargetSet.TargetSetId);
    }

    [Fact]
    public void NamingValidationResult_supports_json_round_trip_serialization()
    {
        var original = NamingEngineTestData.CreateDoorValidationResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<NamingValidationResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Findings!.Count, roundTrip.Findings!.Count);
        Assert.Equal(original.Statistics!.ObjectsPassed, roundTrip.Statistics!.ObjectsPassed);
        Assert.Equal(original.Diagnostics![0].Code, roundTrip.Diagnostics![0].Code);
    }

    [Fact]
    public void Naming_engine_contracts_use_init_only_immutable_properties()
    {
        var contractTypes = typeof(NamingTargetSet).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(NamingTargetSet).Namespace)
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
    public void Naming_engine_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(NamingTargetSet).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Naming", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void INamingEngine_defines_validation_contract_without_implementation()
    {
        var method = Assert.Single(typeof(INamingEngine).GetMethods(), candidate => candidate.Name == "Validate");

        Assert.Equal(typeof(NamingValidationResult), method.ReturnType);
        Assert.Equal(typeof(NamingValidationRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Theory]
    [InlineData(NamingEngineObjectScope.Instance)]
    [InlineData(NamingEngineObjectScope.Type)]
    [InlineData(NamingEngineObjectScope.Family)]
    [InlineData(NamingEngineObjectScope.Category)]
    [InlineData(NamingEngineObjectScope.Model)]
    public void NamingEngineObjectScope_supports_required_scopes(NamingEngineObjectScope scope)
    {
        var criteria = new NamingObjectScopeCriteria
        {
            Scope = scope,
            ObjectIdentifiers = scope == NamingEngineObjectScope.Model ? null : ["object-001"]
        };

        Assert.Equal(scope, criteria.Scope);
    }
}
