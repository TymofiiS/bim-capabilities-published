using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Naming;
using PrefixContracts = BIMCapabilities.Contracts.Engines.Naming.Prefix;

namespace BIMCapabilities.Contracts.Tests;

public class PrefixValidationTests
{
    [Fact]
    public void Prefix_validation_contracts_are_data_only_types()
    {
        var prefixTypes = typeof(PrefixContracts.PrefixValidationRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(PrefixContracts.PrefixValidationRequest).Namespace);

        Assert.All(prefixTypes, type =>
        {
            if (type == typeof(PrefixContracts.IPrefixValidationAtom))
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
    public void PrefixValidationRequest_and_result_can_be_constructed()
    {
        var request = new PrefixContracts.PrefixValidationRequest
        {
            TargetSet = NamingEngineTestData.CreateDoorTargetSet(),
            RequiredPrefixes = [NamingEngineTestData.DoorPrefix],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-prefix-validation-001"
        };

        var result = new PrefixContracts.PrefixValidationResult
        {
            AtomId = "naming.validation.prefix",
            Findings = [],
            Evidence = [],
            Statistics = new PrefixContracts.PrefixValidationStatistics
            {
                ObjectsChecked = 0,
                ObjectsPassed = 0,
                ObjectsFailed = 0,
                MissingPrefixCount = 0,
                InvalidPrefixCount = 0
            },
            Diagnostics =
            [
                new NamingEngineDiagnostic
                {
                    Code = "PrefixValidation.Completed",
                    Message = "Validation completed.",
                    Severity = NamingEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal([NamingEngineTestData.DoorPrefix], request.RequiredPrefixes);
        Assert.Equal("naming.validation.prefix", result.AtomId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void PrefixValidationRequest_supports_json_round_trip_serialization()
    {
        var original = new PrefixContracts.PrefixValidationRequest
        {
            TargetSet = NamingEngineTestData.CreateDoorTargetSet(),
            RequiredPrefixes = [NamingEngineTestData.DoorPrefix, NamingEngineTestData.WindowPrefix],
            CaseSensitive = true,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-prefix-validation-001"
        };

        var json = JsonSerializer.Serialize(original, NamingEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<PrefixContracts.PrefixValidationRequest>(
            json,
            NamingEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RequiredPrefixes, roundTrip.RequiredPrefixes);
        Assert.Equal(original.CaseSensitive, roundTrip.CaseSensitive);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void IPrefixValidationAtom_defines_validation_contract()
    {
        var method = Assert.Single(
            typeof(PrefixContracts.IPrefixValidationAtom).GetMethods(),
            candidate => candidate.Name == "Validate");

        Assert.Equal(typeof(PrefixContracts.PrefixValidationResult), method.ReturnType);
        Assert.Equal(typeof(PrefixContracts.PrefixValidationRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Prefix_validation_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(PrefixContracts.PrefixValidationRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
