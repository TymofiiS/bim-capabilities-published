using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Family.Discovery;
using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;
using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;
using BIMCapabilities.Engines.Family.Atoms.Discovery;
using BIMCapabilities.Engines.Family.Atoms.Filtering;

namespace BIMCapabilities.Engines.Family.Tests;

public class FamilyFilterAtomTests
{
    private static readonly SelectionContracts.FamilySelectionResult SelectionResult = SelectAllForFiltering();

    [Fact]
    public void FilterByCategoryAtom_filters_families_by_category()
    {
        var atom = new FilterByCategoryAtom();
        var result = atom.Filter(CreateRequest(categories: ["Doors"]));

        Assert.Equal(FilterByCategoryAtom.FilterAtomId, result.AtomId);
        Assert.Equal(2, result.FilteredFamilies.Count);
        Assert.All(result.FilteredFamilies, family => Assert.Equal("Doors", family.Category!.Name));
    }

    [Fact]
    public void FilterByNameAtom_filters_families_by_name()
    {
        var atom = new FilterByNameAtom();
        var result = atom.Filter(CreateRequest(exactNames: ["HTL_Window_01"]));

        Assert.Equal(FilterByNameAtom.FilterAtomId, result.AtomId);
        Assert.Single(result.FilteredFamilies);
        Assert.Equal("HTL_Window_01", result.FilteredFamilies[0].Name);
    }

    [Fact]
    public void FilterByParameterAtom_filters_families_by_parameter()
    {
        var atom = new FilterByParameterAtom();
        var result = atom.Filter(CreateRequest(parameterNames: ["FireRating"]));

        Assert.Equal(FilterByParameterAtom.FilterAtomId, result.AtomId);
        Assert.Single(result.FilteredFamilies);
        Assert.Equal("HTL_Door_01", result.FilteredFamilies[0].Name);
    }

    [Fact]
    public void FilterByRelationshipAtom_filters_families_by_relationship()
    {
        var atom = new FilterByRelationshipAtom();
        var result = atom.Filter(CreateRequest(
            relationshipTypes: [NormalizedRelationshipType.Nested],
            targetKind: "family"));

        Assert.Equal(FilterByRelationshipAtom.FilterAtomId, result.AtomId);
        Assert.Single(result.FilteredFamilies);
        Assert.Equal("HTL_Door_01", result.FilteredFamilies[0].Name);
    }

    [Fact]
    public void FilterEmptyFamiliesAtom_removes_empty_families()
    {
        var atom = new FilterEmptyFamiliesAtom();
        var result = atom.Filter(CreateRequest());

        Assert.Equal(FilterEmptyFamiliesAtom.FilterAtomId, result.AtomId);
        Assert.Equal(3, result.FilteredFamilies.Count);
        Assert.DoesNotContain(result.FilteredFamilies, family => family.Name == "HTL_Empty_01");
    }

    [Fact]
    public void FilterUnusedFamiliesAtom_removes_unused_families()
    {
        var atom = new FilterUnusedFamiliesAtom();
        var result = atom.Filter(CreateRequest());

        Assert.Equal(FilterUnusedFamiliesAtom.FilterAtomId, result.AtomId);
        Assert.Equal(3, result.FilteredFamilies.Count);
        Assert.DoesNotContain(result.FilteredFamilies, family => family.Name == "HTL_Unused_01");
    }

    [Fact]
    public void FilterFamiliesCombinedAtom_supports_combined_filtering()
    {
        var atom = new FilterFamiliesCombinedAtom();
        var result = atom.Filter(CreateRequest(
            categories: ["Doors"],
            exactNames: ["HTL_Door_01"],
            parameterNames: ["FireRating"],
            relationshipTypes: [NormalizedRelationshipType.Nested],
            targetKind: "family",
            includeUnused: false));

        Assert.Equal(FilterFamiliesCombinedAtom.FilterAtomId, result.AtomId);
        Assert.Single(result.FilteredFamilies);
        Assert.Equal("HTL_Door_01", result.FilteredFamilies[0].Name);
    }

    [Fact]
    public void Filtering_atoms_generate_statistics()
    {
        var atom = new FilterEmptyFamiliesAtom();
        var result = atom.Filter(CreateRequest());

        Assert.Equal(4, result.Statistics!.CandidateFamilies);
        Assert.Equal(3, result.Statistics.FilteredFamilies);
        Assert.Equal(1, result.Statistics.RemovedFamilies);
    }

    [Fact]
    public void Filtering_atoms_generate_diagnostics()
    {
        var atom = new FilterByNameAtom();
        var result = atom.Filter(CreateRequest(exactNames: ["HTL_Window_01"]));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "FamilyFilter.Completed");
        Assert.Equal("family.discovery.all", result.Metadata!["selectionAtomId"]);
    }

    [Fact]
    public void Filtering_atoms_produce_deterministic_results()
    {
        var atom = new FilterFamiliesCombinedAtom();
        var request = CreateRequest(categories: ["Doors"], parameterNames: ["FireRating"]);

        var first = atom.Filter(request);
        var second = atom.Filter(request);

        Assert.Equal(first.FilteredFamilies[0].Identity.Id, second.FilteredFamilies[0].Identity.Id);
        Assert.Equal(first.Statistics!.FilteredFamilies, second.Statistics!.FilteredFamilies);
        Assert.Equal(first.Diagnostics!.Count, second.Diagnostics!.Count);
    }

    [Fact]
    public void Filtering_atoms_do_not_contain_compliance_or_imported_cad_methods()
    {
        var atomTypes = new[]
        {
            typeof(FilterByCategoryAtom),
            typeof(FilterByNameAtom),
            typeof(FilterByParameterAtom),
            typeof(FilterByRelationshipAtom),
            typeof(FilterEmptyFamiliesAtom),
            typeof(FilterUnusedFamiliesAtom),
            typeof(FilterFamiliesCombinedAtom)
        };

        Assert.All(atomTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Compliance", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ImportedCad", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Filtering_atoms_do_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(FilterByCategoryAtom).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    private static SelectionContracts.FamilySelectionResult SelectAllForFiltering()
    {
        var provider = StubFamilyProvider.CreateForFiltering();
        var discovery = new DiscoverAllFamiliesAtom().Discover(
            new FamilyDiscoveryRequest
            {
                RuleId = "STD-ARC-OPENINGS-V01",
                CorrelationId = "corr-family-filter-001"
            },
            provider);

        return new SelectionContracts.FamilySelectionResult
        {
            AtomId = DiscoverAllFamiliesAtom.DiscoveryAtomId,
            SelectedFamilies = discovery.Families!
        };
    }

    private static FilteringContracts.FamilyFilterRequest CreateRequest(
        IReadOnlyList<string>? categories = null,
        IReadOnlyList<string>? exactNames = null,
        IReadOnlyList<string>? parameterNames = null,
        IReadOnlyList<NormalizedRelationshipType>? relationshipTypes = null,
        string? targetKind = null,
        bool? includeUnused = null)
    {
        return new FilteringContracts.FamilyFilterRequest
        {
            SelectionResult = SelectionResult,
            Criteria = categories is null
                && exactNames is null
                && parameterNames is null
                && relationshipTypes is null
                && targetKind is null
                && includeUnused is null
                ? null
                : new FamilySelectionCriteria
                {
                    Categories = categories is null
                        ? null
                        : new FamilyCategoryCriteria { CategoryNames = categories },
                    Names = exactNames is null
                        ? null
                        : new FamilyNameCriteria { ExactNames = exactNames },
                    Parameters = parameterNames is null
                        ? null
                        : new FamilyParameterCriteria
                        {
                            ParameterNames = parameterNames,
                            MustExist = true
                        },
                    Relationships = relationshipTypes is null && targetKind is null
                        ? null
                        : new FamilyRelationshipCriteria
                        {
                            RelationshipTypes = relationshipTypes,
                            TargetKind = targetKind
                        },
                    Usage = includeUnused is null
                        ? null
                        : new FamilyUsageCriteria { IncludeUnused = includeUnused }
                },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-filter-001"
        };
    }
}
