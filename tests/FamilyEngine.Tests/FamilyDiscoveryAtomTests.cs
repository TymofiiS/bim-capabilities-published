using BIMCapabilities.Contracts.Engines.Family.Discovery;
using BIMCapabilities.Engines.Family.Atoms.Discovery;

namespace BIMCapabilities.Engines.Family.Tests;

public class FamilyDiscoveryAtomTests
{
    private readonly StubFamilyProvider _provider = StubFamilyProvider.CreateDefault();

    [Fact]
    public void DiscoverAllFamiliesAtom_discovers_all_families()
    {
        var atom = new DiscoverAllFamiliesAtom();
        var result = atom.Discover(CreateRequest(), _provider);

        Assert.Equal(DiscoverAllFamiliesAtom.DiscoveryAtomId, result.AtomId);
        Assert.Equal(2, result.Families!.Count);
        Assert.Equal(2, result.Statistics!.DiscoveredFamilies);
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "FamilyDiscovery.Completed");
    }

    [Fact]
    public void DiscoverFamiliesByCategoryAtom_discovers_families_by_category()
    {
        var atom = new DiscoverFamiliesByCategoryAtom();
        var result = atom.Discover(CreateRequest(categoryNames: ["Doors"]), _provider);

        Assert.Equal(DiscoverFamiliesByCategoryAtom.DiscoveryAtomId, result.AtomId);
        Assert.Single(result.Families!);
        Assert.Equal("HTL_Door_01", result.Families![0].Name);
        Assert.Equal(1, result.Statistics!.CountsByCategory!["Doors"]);
    }

    [Fact]
    public void DiscoverFamiliesByNameAtom_discovers_families_by_name()
    {
        var atom = new DiscoverFamiliesByNameAtom();
        var result = atom.Discover(CreateRequest(familyNames: ["HTL_Window_01"]), _provider);

        Assert.Equal(DiscoverFamiliesByNameAtom.DiscoveryAtomId, result.AtomId);
        Assert.Single(result.Families!);
        Assert.Equal("HTL_Window_01", result.Families![0].Name);
    }

    [Fact]
    public void DiscoverFamilyTypesAtom_discovers_family_types()
    {
        var atom = new DiscoverFamilyTypesAtom();
        var result = atom.Discover(CreateRequest(categoryNames: ["Doors"]), _provider);

        Assert.Equal(DiscoverFamilyTypesAtom.DiscoveryAtomId, result.AtomId);
        Assert.Equal(2, result.FamilyTypes!.Count);
        Assert.Equal(2, result.Statistics!.DiscoveredFamilyTypes);
        Assert.Contains(result.FamilyTypes, familyType => familyType.Name == "HTL_Door_01_900x2100");
    }

    [Fact]
    public void Discovery_atoms_generate_statistics_and_provider_diagnostics()
    {
        var atom = new DiscoverAllFamiliesAtom();
        var result = atom.Discover(CreateRequest(), _provider);

        Assert.Equal(2, result.Statistics!.ProviderRetrievedFamilies);
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "FamilyProvider.Stub");
        Assert.Equal("stub-family-provider", result.Metadata!["providerId"]);
    }

    [Fact]
    public void Discovery_atoms_produce_deterministic_results()
    {
        var atom = new DiscoverFamiliesByCategoryAtom();
        var request = CreateRequest(categoryNames: ["Doors"]);

        var first = atom.Discover(request, _provider);
        var second = atom.Discover(request, _provider);

        Assert.Equal(first.Families![0].Identity.Id, second.Families![0].Identity.Id);
        Assert.Equal(first.Statistics!.DiscoveredFamilies, second.Statistics!.DiscoveredFamilies);
        Assert.Equal(first.Diagnostics!.Count, second.Diagnostics!.Count);
    }

    [Fact]
    public void Discovery_atoms_do_not_contain_filter_or_compliance_methods()
    {
        var atomTypes = new[]
        {
            typeof(DiscoverAllFamiliesAtom),
            typeof(DiscoverFamiliesByCategoryAtom),
            typeof(DiscoverFamiliesByNameAtom),
            typeof(DiscoverFamilyTypesAtom)
        };

        Assert.All(atomTypes, type =>
        {
            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Filter", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Compliance", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ImportedCad", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Select", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    private static FamilyDiscoveryRequest CreateRequest(
        IReadOnlyList<string>? categoryNames = null,
        IReadOnlyList<string>? familyNames = null)
    {
        return new FamilyDiscoveryRequest
        {
            CategoryNames = categoryNames,
            FamilyNames = familyNames,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-discovery-001"
        };
    }
}
