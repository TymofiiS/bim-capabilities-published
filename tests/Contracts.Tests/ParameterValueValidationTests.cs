using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Evidence;
using ValueContracts = BIMCapabilities.Contracts.Engines.Parameter.Value;

namespace BIMCapabilities.Contracts.Tests;

public class ParameterValueValidationTests
{
    [Fact]
    public void Parameter_value_contracts_are_data_only_types()
    {
        var valueTypes = typeof(ValueContracts.ParameterValueValidationRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ValueContracts.ParameterValueValidationRequest).Namespace);

        Assert.All(valueTypes, type =>
        {
            if (type == typeof(ValueContracts.IParameterValueValidationAtom))
            {
                return;
            }

            if (type.IsEnum)
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
    public void ParameterValueValidationRequest_and_result_can_be_constructed()
    {
        var request = new ValueContracts.ParameterValueValidationRequest
        {
            TargetSet = ParameterEngineTestData.CreateDoorTargetSet(),
            Rules =
            [
                new ValueContracts.ParameterValueRule
                {
                    ParameterName = "FireRating",
                    RequiredValue = true,
                    Severity = EvidenceSeverity.Error
                },
                new ValueContracts.ParameterValueRule
                {
                    ParameterName = "RoomName",
                    RequiredValue = true
                }
            ],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-value-001"
        };

        var result = new ValueContracts.ParameterValueValidationResult
        {
            AtomId = "parameter.validation.value",
            Findings = [],
            Evidence = [],
            Statistics = new ValueContracts.ParameterValueValidationStatistics
            {
                ObjectsChecked = 0,
                ObjectsPassed = 0,
                ObjectsFailed = 0,
                ParametersChecked = 0,
                InvalidValues = 0,
                MissingValues = 0
            },
            Diagnostics =
            [
                new ParameterEngineDiagnostic
                {
                    Code = "ParameterValueValidation.Completed",
                    Message = "Validation completed.",
                    Severity = ParameterEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal(2, request.Rules.Count);
        Assert.Equal("parameter.validation.value", result.AtomId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void ParameterValueRule_supports_all_validation_constraints()
    {
        var rule = new ValueContracts.ParameterValueRule
        {
            ParameterName = "FireRating",
            RequiredValue = true,
            AllowedValues = ["30", "60", "90"],
            ForbiddenValues = ["N/A"],
            MinimumLength = 1,
            MaximumLength = 10,
            RegularExpression = @"^\d+$",
            CustomRuleIdentifier = "client.fire-rating.standard",
            Severity = EvidenceSeverity.Warning
        };

        Assert.True(rule.RequiredValue);
        Assert.Equal(EvidenceSeverity.Warning, rule.Severity);
        Assert.Equal("client.fire-rating.standard", rule.CustomRuleIdentifier);
    }

    [Fact]
    public void ParameterValueValidationRequest_supports_json_round_trip_serialization()
    {
        var original = new ValueContracts.ParameterValueValidationRequest
        {
            TargetSet = ParameterEngineTestData.CreateDoorTargetSet(),
            ParameterQueryResult = new ParameterQueryResult { Parameters = [] },
            Rules =
            [
                new ValueContracts.ParameterValueRule
                {
                    ParameterName = "Manufacturer",
                    RequiredValue = true
                }
            ],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-value-001"
        };

        var json = JsonSerializer.Serialize(original, ParameterEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<ValueContracts.ParameterValueValidationRequest>(
            json,
            ParameterEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Rules[0].ParameterName, roundTrip.Rules[0].ParameterName);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void IParameterValueValidationAtom_defines_validation_contract()
    {
        var method = Assert.Single(
            typeof(ValueContracts.IParameterValueValidationAtom).GetMethods(),
            candidate => candidate.Name == "Validate");

        Assert.Equal(typeof(ValueContracts.ParameterValueValidationResult), method.ReturnType);
        Assert.Equal(typeof(ValueContracts.ParameterValueValidationRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Parameter_value_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ValueContracts.ParameterValueValidationRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
