using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Family;

namespace BIMCapabilities.Contracts.Tests;

public class FamilyEngineTests
{
    private static readonly JsonSerializerOptions JsonOptions = FamilyEngineSerialization.Options;

    [Fact]
    public void Family_engine_contracts_are_data_only_types()
    {
        var familyEngineTypes = typeof(FamilyTargetSet).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(FamilyTargetSet).Namespace);

        Assert.All(familyEngineTypes, type =>
        {
            if (type == typeof(IFamilyEngine))
            {
                return;
            }

            if (type == typeof(FamilyEngineDiagnosticSeverity))
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
    public void FamilyTargetSet_can_be_constructed_with_required_properties()
    {
        var targetSet = FamilyEngineTestData.CreateDoorTargetSet();

        Assert.Equal("target-set-doors-001", targetSet.TargetSetId);
        Assert.Single(targetSet.Families!);
        Assert.Equal("HTL_Door_01", targetSet.Families![0].Name);
        Assert.Single(targetSet.FamilyTypes!);
        Assert.Single(targetSet.Categories!);
        Assert.Equal("Doors", targetSet.Categories![0].Name);
        Assert.Single(targetSet.Relationships!);
        Assert.Equal("selectedFamilies", targetSet.Metadata!["scope"]);
    }

    [Fact]
    public void FamilySelectionCriteria_supports_category_name_parameter_relationship_usage_and_custom_criteria()
    {
        var criteria = FamilyEngineTestData.CreateDoorSelectionCriteria();

        Assert.Equal(["Doors"], criteria.Categories!.CategoryNames);
        Assert.Equal("HTL_Door_*", criteria.Names!.NamePattern);
        Assert.Equal(["FireRating"], criteria.Parameters!.ParameterNames);
        Assert.True(criteria.Parameters.MustExist);
        Assert.Equal(Adapters.Revit.Translation.NormalizedRelationshipType.Nested, criteria.Relationships!.RelationshipTypes![0]);
        Assert.True(criteria.Usage!.IncludeNested);
        Assert.Equal("openings-validation", criteria.Custom!.Properties!["selectionPurpose"]);
    }

    [Fact]
    public void FamilySelectionRequest_can_be_constructed_with_required_properties()
    {
        var request = FamilyEngineTestData.CreateDoorSelectionRequest();

        Assert.Equal("STD-ARC-OPENINGS-V01", request.RuleId);
        Assert.Equal("corr-family-engine-001", request.CorrelationId);
        Assert.NotNull(request.SourceTargetSet);
        Assert.Equal(["Doors"], request.Criteria.Categories!.CategoryNames);
    }

    [Fact]
    public void FamilySelectionResult_supports_selected_families_diagnostics_statistics_and_metadata()
    {
        var result = FamilyEngineTestData.CreateDoorSelectionResult();

        Assert.Equal("selected-doors-001", result.SelectedFamilies.TargetSetId);
        Assert.Single(result.SelectedFamilies.Families!);
        Assert.Single(result.Diagnostics!);
        Assert.Equal(1, result.Statistics!.SelectedFamilies);
        Assert.Equal("STD-ARC-OPENINGS-V01", result.Metadata!["ruleId"]);
    }

    [Fact]
    public void FamilyEngineDiagnostic_supports_required_structure()
    {
        var diagnostic = new FamilyEngineDiagnostic
        {
            Code = "FamilyEngine.UnsupportedCriteria",
            Message = "Custom criteria key is not recognized.",
            Severity = FamilyEngineDiagnosticSeverity.Warning,
            Location = "criteria:custom",
            Data = new Dictionary<string, string>
            {
                ["property"] = "unknownKey"
            }
        };

        Assert.Equal(FamilyEngineDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("unknownKey", diagnostic.Data!["property"]);
    }

    [Fact]
    public void FamilySelectionRequest_supports_json_round_trip_serialization()
    {
        var original = FamilyEngineTestData.CreateDoorSelectionRequest();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<FamilySelectionRequest>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RuleId, roundTrip.RuleId);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(original.Criteria.Names!.NamePattern, roundTrip.Criteria.Names!.NamePattern);
        Assert.Equal(original.SourceTargetSet!.TargetSetId, roundTrip.SourceTargetSet!.TargetSetId);
    }

    [Fact]
    public void FamilySelectionResult_supports_json_round_trip_serialization()
    {
        var original = FamilyEngineTestData.CreateDoorSelectionResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<FamilySelectionResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.SelectedFamilies.TargetSetId, roundTrip.SelectedFamilies.TargetSetId);
        Assert.Equal(original.SelectedFamilies.Families!.Count, roundTrip.SelectedFamilies.Families!.Count);
        Assert.Equal(original.Statistics!.SelectedFamilies, roundTrip.Statistics!.SelectedFamilies);
    }

    [Fact]
    public void Family_engine_contracts_use_init_only_immutable_properties()
    {
        var contractTypes = typeof(FamilyTargetSet).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(FamilyTargetSet).Namespace)
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
    public void Family_engine_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(FamilyTargetSet).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Family", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void IFamilyEngine_defines_selection_contract_without_implementation()
    {
        var method = Assert.Single(typeof(IFamilyEngine).GetMethods(), candidate => candidate.Name == "Select");

        Assert.Equal(typeof(FamilySelectionResult), method.ReturnType);
        Assert.Equal(typeof(FamilySelectionRequest), method.GetParameters()[0].ParameterType);
    }
}
