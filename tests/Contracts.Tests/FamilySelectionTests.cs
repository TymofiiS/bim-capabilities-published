using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Family.Discovery;
using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Contracts.Tests;

public class FamilySelectionTests
{
    [Fact]
    public void Family_selection_contracts_are_data_only_types()
    {
        var selectionTypes = typeof(SelectionContracts.FamilySelectionRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(SelectionContracts.FamilySelectionRequest).Namespace);

        Assert.All(selectionTypes, type =>
        {
            if (type == typeof(SelectionContracts.IFamilySelectionAtom))
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
    public void FamilySelectionRequest_and_result_can_be_constructed()
    {
        var request = new SelectionContracts.FamilySelectionRequest
        {
            DiscoveryResult = new FamilyDiscoveryResult
            {
                AtomId = "family.discovery.all",
                Families = []
            },
            Criteria = new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Doors"] }
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-selection-001"
        };

        var result = new SelectionContracts.FamilySelectionResult
        {
            AtomId = "family.selection.by-category",
            SelectedFamilies = [],
            Statistics = new SelectionContracts.FamilySelectionStatistics
            {
                CandidateFamilies = 0,
                SelectedFamilies = 0,
                RejectedFamilies = 0
            },
            Diagnostics =
            [
                new FamilyEngineDiagnostic
                {
                    Code = "FamilySelectionContracts.Completed",
                    Message = "Selection completed.",
                    Severity = FamilyEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal(["Doors"], request.Criteria!.Categories!.CategoryNames);
        Assert.Equal("family.selection.by-category", result.AtomId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void FamilySelectionRequest_supports_json_round_trip_serialization()
    {
        var original = new SelectionContracts.FamilySelectionRequest
        {
            DiscoveryResult = new FamilyDiscoveryResult
            {
                AtomId = "family.discovery.all",
                Families = []
            },
            Criteria = new FamilySelectionCriteria
            {
                Names = new FamilyNameCriteria { ExactNames = ["HTL_Door_01"] }
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-selection-001"
        };

        var json = JsonSerializer.Serialize(original, FamilyEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<SelectionContracts.FamilySelectionRequest>(json, FamilyEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Criteria!.Names!.ExactNames, roundTrip.Criteria!.Names!.ExactNames);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void IFamilySelectionAtom_defines_selection_contract()
    {
        var method = Assert.Single(typeof(SelectionContracts.IFamilySelectionAtom).GetMethods(), candidate => candidate.Name == "Select");

        Assert.Equal(typeof(SelectionContracts.FamilySelectionResult), method.ReturnType);
        Assert.Equal(typeof(SelectionContracts.FamilySelectionRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Family_selection_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(SelectionContracts.FamilySelectionRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
