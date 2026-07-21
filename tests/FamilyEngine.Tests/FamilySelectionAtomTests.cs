using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Family.Discovery;
using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;
using BIMCapabilities.Engines.Family.Atoms.Discovery;
using BIMCapabilities.Engines.Family.Atoms.Selection;

namespace BIMCapabilities.Engines.Family.Tests;

public class FamilySelectionAtomTests
{
    private static readonly FamilyDiscoveryResult DiscoveryResult = DiscoverAll();

    [Fact]
    public void SelectFamiliesByCategoryAtom_selects_families_by_category()
    {
        var atom = new SelectFamiliesByCategoryAtom();
        var result = atom.Select(CreateRequest(categories: ["Doors"]));

        Assert.Equal(SelectFamiliesByCategoryAtom.SelectionAtomId, result.AtomId);
        Assert.Single(result.SelectedFamilies);
        Assert.Equal("HTL_Door_01", result.SelectedFamilies[0].Name);
        Assert.Equal(1, result.Statistics!.CountsByCategory!["Doors"]);
    }

    [Fact]
    public void SelectFamiliesByNameAtom_selects_families_by_name()
    {
        var atom = new SelectFamiliesByNameAtom();
        var result = atom.Select(CreateRequest(exactNames: ["HTL_Window_01"]));

        Assert.Equal(SelectFamiliesByNameAtom.SelectionAtomId, result.AtomId);
        Assert.Single(result.SelectedFamilies);
        Assert.Equal("HTL_Window_01", result.SelectedFamilies[0].Name);
    }

    [Fact]
    public void SelectFamiliesByParameterAtom_selects_families_by_parameter()
    {
        var atom = new SelectFamiliesByParameterAtom();
        var result = atom.Select(CreateRequest(parameterNames: ["FireRating"]));

        Assert.Equal(SelectFamiliesByParameterAtom.SelectionAtomId, result.AtomId);
        Assert.Single(result.SelectedFamilies);
        Assert.Equal("HTL_Door_01", result.SelectedFamilies[0].Name);
    }

    [Fact]
    public void SelectFamiliesByRelationshipAtom_selects_families_by_relationship()
    {
        var atom = new SelectFamiliesByRelationshipAtom();
        var result = atom.Select(CreateRequest(
            relationshipTypes: [NormalizedRelationshipType.Nested],
            targetKind: "family"));

        Assert.Equal(SelectFamiliesByRelationshipAtom.SelectionAtomId, result.AtomId);
        Assert.Single(result.SelectedFamilies);
        Assert.Equal("HTL_Door_01", result.SelectedFamilies[0].Name);
    }

    [Fact]
    public void SelectFamiliesCombinedAtom_supports_combined_selection()
    {
        var atom = new SelectFamiliesCombinedAtom();
        var result = atom.Select(CreateRequest(
            categories: ["Doors"],
            exactNames: ["HTL_Door_01"],
            parameterNames: ["FireRating"],
            relationshipTypes: [NormalizedRelationshipType.Nested],
            targetKind: "family"));

        Assert.Equal(SelectFamiliesCombinedAtom.SelectionAtomId, result.AtomId);
        Assert.Single(result.SelectedFamilies);
        Assert.Equal("HTL_Door_01", result.SelectedFamilies[0].Name);
    }

    [Fact]
    public void Selection_atoms_generate_statistics()
    {
        var atom = new SelectFamiliesByCategoryAtom();
        var result = atom.Select(CreateRequest(categories: ["Doors"]));

        Assert.Equal(2, result.Statistics!.CandidateFamilies);
        Assert.Equal(1, result.Statistics.SelectedFamilies);
        Assert.Equal(1, result.Statistics.RejectedFamilies);
    }

    [Fact]
    public void Selection_atoms_generate_diagnostics()
    {
        var atom = new SelectFamiliesByNameAtom();
        var result = atom.Select(CreateRequest(exactNames: ["HTL_Window_01"]));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "FamilySelectionContracts.Completed");
        Assert.Equal("family.discovery.all", result.Metadata!["discoveryAtomId"]);
    }

    [Fact]
    public void Selection_atoms_produce_deterministic_results()
    {
        var atom = new SelectFamiliesCombinedAtom();
        var request = CreateRequest(categories: ["Doors"], parameterNames: ["FireRating"]);

        var first = atom.Select(request);
        var second = atom.Select(request);

        Assert.Equal(first.SelectedFamilies[0].Identity.Id, second.SelectedFamilies[0].Identity.Id);
        Assert.Equal(first.Statistics!.SelectedFamilies, second.Statistics!.SelectedFamilies);
        Assert.Equal(first.Diagnostics!.Count, second.Diagnostics!.Count);
    }

    [Fact]
    public void Selection_atoms_do_not_contain_filter_compliance_or_imported_cad_methods()
    {
        var atomTypes = new[]
        {
            typeof(SelectFamiliesByCategoryAtom),
            typeof(SelectFamiliesByNameAtom),
            typeof(SelectFamiliesByParameterAtom),
            typeof(SelectFamiliesByRelationshipAtom),
            typeof(SelectFamiliesCombinedAtom)
        };

        Assert.All(atomTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Filter", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Compliance", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ImportedCad", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Selection_atoms_do_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(SelectFamiliesByCategoryAtom).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    private static FamilyDiscoveryResult DiscoverAll()
    {
        var provider = StubFamilyProvider.CreateDefault();
        return new DiscoverAllFamiliesAtom().Discover(
            new FamilyDiscoveryRequest
            {
                RuleId = "STD-ARC-OPENINGS-V01",
                CorrelationId = "corr-family-selection-001"
            },
            provider);
    }

    private static SelectionContracts.FamilySelectionRequest CreateRequest(
        IReadOnlyList<string>? categories = null,
        IReadOnlyList<string>? exactNames = null,
        IReadOnlyList<string>? parameterNames = null,
        IReadOnlyList<NormalizedRelationshipType>? relationshipTypes = null,
        string? targetKind = null)
    {
        return new SelectionContracts.FamilySelectionRequest
        {
            DiscoveryResult = DiscoveryResult,
            Criteria = new FamilySelectionCriteria
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
                    }
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-selection-001"
        };
    }
}
