using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Family;
using TargetSetContracts = BIMCapabilities.Contracts.Engines.Family.TargetSet;

namespace BIMCapabilities.Contracts.Tests;

public class FamilyTargetSetGeneratorTests
{
    [Fact]
    public void Family_target_set_contracts_are_data_only_types()
    {
        var targetSetTypes = typeof(TargetSetContracts.FamilyTargetSetRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(TargetSetContracts.FamilyTargetSetRequest).Namespace);

        Assert.All(targetSetTypes, type =>
        {
            if (type == typeof(TargetSetContracts.IFamilyTargetSetGenerator)
                || type == typeof(TargetSetContracts.ImportedCadComplianceMode))
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
    public void FamilyTargetSetRequest_and_result_can_be_constructed()
    {
        var request = new TargetSetContracts.FamilyTargetSetRequest
        {
            Definition = new TargetSetContracts.TargetSetDefinition
            {
                Name = "All Door Families",
                Description = "Door families for parameter validation.",
                SelectionCriteria = new FamilySelectionCriteria
                {
                    Categories = new FamilyCategoryCriteria { CategoryNames = ["Doors"] }
                }
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-target-set-001"
        };

        var result = new TargetSetContracts.FamilyTargetSetResult
        {
            GeneratorId = "family.target-set.generator",
            TargetSet = new FamilyTargetSet
            {
                TargetSetId = "target-set-all-door-families",
                Families = []
            },
            Statistics = new TargetSetContracts.FamilyTargetSetStatistics
            {
                DiscoveredFamilies = 0,
                TargetFamilies = 0
            },
            Diagnostics =
            [
                new FamilyEngineDiagnostic
                {
                    Code = "FamilyTargetSet.Completed",
                    Message = "Target set generation completed.",
                    Severity = FamilyEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal("All Door Families", request.Definition.Name);
        Assert.Equal("family.target-set.generator", result.GeneratorId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void FamilyTargetSetRequest_supports_json_round_trip_serialization()
    {
        var original = new TargetSetContracts.FamilyTargetSetRequest
        {
            Definition = new TargetSetContracts.TargetSetDefinition
            {
                Name = "All Window Families",
                SelectionCriteria = new FamilySelectionCriteria
                {
                    Categories = new FamilyCategoryCriteria { CategoryNames = ["Windows"] }
                },
                ComplianceCriteria = new TargetSetContracts.TargetSetComplianceCriteria
                {
                    ImportedCadMode = TargetSetContracts.ImportedCadComplianceMode.ExcludeImportedCad
                }
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-target-set-001"
        };

        var json = JsonSerializer.Serialize(original, FamilyEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<TargetSetContracts.FamilyTargetSetRequest>(json, FamilyEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Definition.Name, roundTrip.Definition.Name);
        Assert.Equal(
            TargetSetContracts.ImportedCadComplianceMode.ExcludeImportedCad,
            roundTrip.Definition.ComplianceCriteria!.ImportedCadMode);
    }

    [Fact]
    public void IFamilyTargetSetGenerator_defines_generation_contract()
    {
        var method = Assert.Single(typeof(TargetSetContracts.IFamilyTargetSetGenerator).GetMethods(), candidate => candidate.Name == "Generate");

        Assert.Equal(typeof(TargetSetContracts.FamilyTargetSetResult), method.ReturnType);
        Assert.Equal(typeof(TargetSetContracts.FamilyTargetSetRequest), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(Adapters.Revit.Read.IFamilyProvider), method.GetParameters()[1].ParameterType);
    }

    [Fact]
    public void Family_target_set_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(TargetSetContracts.FamilyTargetSetRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
