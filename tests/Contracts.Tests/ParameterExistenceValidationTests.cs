using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter;
using ExistenceContracts = BIMCapabilities.Contracts.Engines.Parameter.Existence;

namespace BIMCapabilities.Contracts.Tests;

public class ParameterExistenceValidationTests
{
    [Fact]
    public void Parameter_existence_contracts_are_data_only_types()
    {
        var existenceTypes = typeof(ExistenceContracts.ParameterExistenceRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ExistenceContracts.ParameterExistenceRequest).Namespace);

        Assert.All(existenceTypes, type =>
        {
            if (type == typeof(ExistenceContracts.IParameterExistenceValidationAtom))
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
    public void ParameterExistenceRequest_and_result_can_be_constructed()
    {
        var request = new ExistenceContracts.ParameterExistenceRequest
        {
            TargetSet = ParameterEngineTestData.CreateDoorTargetSet(),
            RequiredParameterNames = ["FireRating", "RoomName"],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-existence-001"
        };

        var result = new ExistenceContracts.ParameterExistenceResult
        {
            AtomId = "parameter.validation.existence",
            Findings = [],
            Evidence = [],
            Statistics = new ExistenceContracts.ParameterExistenceStatistics
            {
                ObjectsChecked = 0,
                ObjectsPassed = 0,
                ObjectsFailed = 0,
                MissingParameters = 0
            },
            Diagnostics =
            [
                new ParameterEngineDiagnostic
                {
                    Code = "ParameterExistence.Completed",
                    Message = "Validation completed.",
                    Severity = ParameterEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal(["FireRating", "RoomName"], request.RequiredParameterNames);
        Assert.Equal("parameter.validation.existence", result.AtomId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void ParameterExistenceRequest_supports_json_round_trip_serialization()
    {
        var original = new ExistenceContracts.ParameterExistenceRequest
        {
            TargetSet = ParameterEngineTestData.CreateDoorTargetSet(),
            ParameterQueryResult = new ParameterQueryResult { Parameters = [] },
            RequiredParameterNames = ["Manufacturer"],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-existence-001"
        };

        var json = JsonSerializer.Serialize(original, ParameterEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<ExistenceContracts.ParameterExistenceRequest>(json, ParameterEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RequiredParameterNames, roundTrip.RequiredParameterNames);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void IParameterExistenceValidationAtom_defines_validation_contract()
    {
        var method = Assert.Single(typeof(ExistenceContracts.IParameterExistenceValidationAtom).GetMethods(), candidate => candidate.Name == "Validate");

        Assert.Equal(typeof(ExistenceContracts.ParameterExistenceResult), method.ReturnType);
        Assert.Equal(typeof(ExistenceContracts.ParameterExistenceRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Parameter_existence_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ExistenceContracts.ParameterExistenceRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
