using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Family;
using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;
using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Contracts.Tests;

public class FamilyFilteringTests
{
    [Fact]
    public void Family_filtering_contracts_are_data_only_types()
    {
        var filteringTypes = typeof(FilteringContracts.FamilyFilterRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(FilteringContracts.FamilyFilterRequest).Namespace);

        Assert.All(filteringTypes, type =>
        {
            if (type == typeof(FilteringContracts.IFamilyFilterAtom))
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
    public void FamilyFilterRequest_and_result_can_be_constructed()
    {
        var request = new FilteringContracts.FamilyFilterRequest
        {
            SelectionResult = new SelectionContracts.FamilySelectionResult
            {
                AtomId = "family.selection.combined",
                SelectedFamilies = []
            },
            Criteria = new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Doors"] }
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-filter-001"
        };

        var result = new FilteringContracts.FamilyFilterResult
        {
            AtomId = "family.filter.by-category",
            FilteredFamilies = [],
            Statistics = new FilteringContracts.FamilyFilterStatistics
            {
                CandidateFamilies = 0,
                FilteredFamilies = 0,
                RemovedFamilies = 0
            },
            Diagnostics =
            [
                new FamilyEngineDiagnostic
                {
                    Code = "FamilyFilter.Completed",
                    Message = "Filtering completed.",
                    Severity = FamilyEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal(["Doors"], request.Criteria!.Categories!.CategoryNames);
        Assert.Equal("family.filter.by-category", result.AtomId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void FamilyFilterRequest_supports_json_round_trip_serialization()
    {
        var original = new FilteringContracts.FamilyFilterRequest
        {
            SelectionResult = new SelectionContracts.FamilySelectionResult
            {
                AtomId = "family.selection.combined",
                SelectedFamilies = []
            },
            Criteria = new FamilySelectionCriteria
            {
                Names = new FamilyNameCriteria { ExactNames = ["HTL_Door_01"] }
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-filter-001"
        };

        var json = JsonSerializer.Serialize(original, FamilyEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<FilteringContracts.FamilyFilterRequest>(json, FamilyEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Criteria!.Names!.ExactNames, roundTrip.Criteria!.Names!.ExactNames);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void IFamilyFilterAtom_defines_filter_contract()
    {
        var method = Assert.Single(typeof(FilteringContracts.IFamilyFilterAtom).GetMethods(), candidate => candidate.Name == "Filter");

        Assert.Equal(typeof(FilteringContracts.FamilyFilterResult), method.ReturnType);
        Assert.Equal(typeof(FilteringContracts.FamilyFilterRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Family_filtering_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(FilteringContracts.FamilyFilterRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
