using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter;
using EngineSharedParameterFileReference = BIMCapabilities.Contracts.Engines.Parameter.ParameterSharedParameterFileReference;
using SharedParameterContracts = BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

namespace BIMCapabilities.Contracts.Tests;

public class SharedParameterValidationTests
{
    [Fact]
    public void Shared_parameter_contracts_are_data_only_types()
    {
        var sharedParameterTypes = typeof(SharedParameterContracts.SharedParameterValidationRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(SharedParameterContracts.SharedParameterValidationRequest).Namespace);

        Assert.All(sharedParameterTypes, type =>
        {
            if (type == typeof(SharedParameterContracts.ISharedParameterValidationAtom))
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
    public void SharedParameterValidationRequest_and_result_can_be_constructed()
    {
        var request = new SharedParameterContracts.SharedParameterValidationRequest
        {
            TargetSet = ParameterEngineTestData.CreateDoorTargetSet(),
            SharedParameterFile = new EngineSharedParameterFileReference
            {
                FilePath = ParameterEngineTestData.DemoSharedParameterFilePath
            },
            ParameterNamesToValidate = ["FireRating", "RoomName"],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-shared-parameter-001"
        };

        var result = new SharedParameterContracts.SharedParameterValidationResult
        {
            AtomId = "parameter.validation.shared-parameter",
            LoadedDefinitions =
            [
                new SharedParameterContracts.SharedParameterDefinition
                {
                    Name = "FireRating",
                    Guid = "f1a2b3c4-d5e6-7890-abcd-ef1234567890",
                    DataType = "TEXT",
                    Group = "Data"
                }
            ],
            Findings = [],
            Evidence = [],
            Statistics = new SharedParameterContracts.SharedParameterValidationStatistics
            {
                ObjectsChecked = 0,
                ObjectsPassed = 0,
                ObjectsFailed = 0,
                SharedParametersChecked = 0,
                MissingSharedParameters = 0,
                InvalidSharedParameters = 0
            },
            Diagnostics =
            [
                new ParameterEngineDiagnostic
                {
                    Code = "SharedParameterValidation.Completed",
                    Message = "Validation completed.",
                    Severity = ParameterEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal(["FireRating", "RoomName"], request.ParameterNamesToValidate);
        Assert.Equal("parameter.validation.shared-parameter", result.AtomId);
        Assert.Single(result.Diagnostics!);
        Assert.Single(result.LoadedDefinitions!);
    }

    [Fact]
    public void SharedParameterValidationRequest_supports_json_round_trip_serialization()
    {
        var original = new SharedParameterContracts.SharedParameterValidationRequest
        {
            TargetSet = ParameterEngineTestData.CreateDoorTargetSet(),
            ParameterQueryResult = new ParameterQueryResult { Parameters = [] },
            SharedParameterFile = new EngineSharedParameterFileReference
            {
                FilePath = ParameterEngineTestData.DemoSharedParameterFilePath
            },
            ParameterNamesToValidate = ["Manufacturer"],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-shared-parameter-001"
        };

        var json = JsonSerializer.Serialize(original, ParameterEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<SharedParameterContracts.SharedParameterValidationRequest>(
            json,
            ParameterEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.ParameterNamesToValidate, roundTrip.ParameterNamesToValidate);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(original.SharedParameterFile.FilePath, roundTrip.SharedParameterFile.FilePath);
    }

    [Fact]
    public void ISharedParameterValidationAtom_defines_validation_contract()
    {
        var method = Assert.Single(
            typeof(SharedParameterContracts.ISharedParameterValidationAtom).GetMethods(),
            candidate => candidate.Name == "Validate");

        Assert.Equal(typeof(SharedParameterContracts.SharedParameterValidationResult), method.ReturnType);
        Assert.Equal(typeof(SharedParameterContracts.SharedParameterValidationRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Shared_parameter_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(SharedParameterContracts.SharedParameterValidationRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
