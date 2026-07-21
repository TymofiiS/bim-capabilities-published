using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Evidence;
using TargetSetContracts = BIMCapabilities.Contracts.Engines.Family.TargetSet;
using BIMCapabilities.Engines.Family.Generation;

namespace BIMCapabilities.Engines.Family.Tests;

public class FamilyTargetSetGeneratorTests
{
    private static readonly StubFamilyProvider Provider = StubFamilyProvider.CreateForTargetSetGeneration();
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Generate_creates_door_family_target_set()
    {
        var generator = new FamilyTargetSetGenerator();
        var result = generator.Generate(CreateRequest(
            name: "All Door Families",
            selection: new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Doors"] }
            }), Provider);

        Assert.Equal(FamilyTargetSetGenerator.TargetSetGeneratorId, result.GeneratorId);
        Assert.Equal(2, result.TargetSet.Families!.Count);
        Assert.All(result.TargetSet.Families, family => Assert.Equal("Doors", family.Category!.Name));
        Assert.Equal(2, result.Statistics!.TargetFamilies);
    }

    [Fact]
    public void Generate_creates_window_family_target_set()
    {
        var generator = new FamilyTargetSetGenerator();
        var result = generator.Generate(CreateRequest(
            name: "All Window Families",
            selection: new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Windows"] }
            }), Provider);

        Assert.Single(result.TargetSet.Families!);
        Assert.Equal("HTL_Window_01", result.TargetSet.Families![0].Name);
        Assert.Equal("Windows", result.TargetSet.Categories![0].Name);
    }

    [Fact]
    public void Generate_creates_imported_cad_target_set()
    {
        var generator = new FamilyTargetSetGenerator();
        var result = generator.Generate(CreateRequest(
            name: "All Families containing Imported CAD",
            compliance: new TargetSetContracts.TargetSetComplianceCriteria
            {
                ImportedCadMode = TargetSetContracts.ImportedCadComplianceMode.RequireImportedCad
            }), Provider);

        Assert.Single(result.TargetSet.Families!);
        Assert.Equal("HTL_Door_01", result.TargetSet.Families![0].Name);
        Assert.NotEmpty(result.Evidence!);
        Assert.Equal(1, result.Statistics!.ImportedCadReferencesFound);
    }

    [Fact]
    public void Generate_creates_no_imported_cad_target_set()
    {
        var generator = new FamilyTargetSetGenerator();
        var result = generator.Generate(CreateRequest(
            name: "All Families without Imported CAD",
            compliance: new TargetSetContracts.TargetSetComplianceCriteria
            {
                ImportedCadMode = TargetSetContracts.ImportedCadComplianceMode.ExcludeImportedCad
            }), Provider);

        Assert.Equal(2, result.TargetSet.Families!.Count);
        Assert.DoesNotContain(result.TargetSet.Families, family => family.Name == "HTL_Door_01");
        Assert.Contains(result.TargetSet.Families, family => family.Name == "HTL_Door_02");
        Assert.Contains(result.TargetSet.Families, family => family.Name == "HTL_Window_01");
    }

    [Fact]
    public void Generate_supports_combined_criteria_target_set()
    {
        var generator = new FamilyTargetSetGenerator();
        var result = generator.Generate(CreateRequest(
            name: "All Door Families with FireRating",
            selection: new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Doors"] },
                Parameters = new FamilyParameterCriteria
                {
                    ParameterNames = ["FireRating"],
                    MustExist = true
                }
            },
            filtering: new FamilySelectionCriteria
            {
                Usage = new FamilyUsageCriteria { IncludeUnused = false }
            }), Provider);

        Assert.Equal(2, result.TargetSet.Families!.Count);
        Assert.All(result.TargetSet.Families, family =>
            Assert.Contains("FireRating", CollectParameterNames(family)));
    }

    [Fact]
    public void Generate_propagates_evidence_from_imported_cad_detection()
    {
        var generator = new FamilyTargetSetGenerator();
        var result = generator.Generate(CreateRequest(
            name: "All Door Families",
            selection: new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Doors"] }
            }), Provider);

        Assert.Single(result.Evidence!);
        Assert.Equal(EvidenceCategory.Validation, result.Evidence![0].Category);
        Assert.Contains("imported-cad-family-001", result.Evidence[0].EvidenceId, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_propagates_diagnostics_from_pipeline()
    {
        var generator = new FamilyTargetSetGenerator();
        var result = generator.Generate(CreateRequest(
            name: "All Window Families",
            selection: new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Windows"] }
            }), Provider);

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "FamilyDiscovery.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "FamilySelectionContracts.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ImportedCadDetection.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "FamilyTargetSet.Completed");
    }

    [Fact]
    public void Generate_generates_statistics()
    {
        var generator = new FamilyTargetSetGenerator();
        var result = generator.Generate(CreateRequest(
            name: "All Window Families with AcousticRating",
            selection: new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Windows"] },
                Parameters = new FamilyParameterCriteria
                {
                    ParameterNames = ["AcousticRating"],
                    MustExist = true
                }
            }), Provider);

        Assert.Equal(3, result.Statistics!.DiscoveredFamilies);
        Assert.Equal(1, result.Statistics.SelectedFamilies);
        Assert.Equal(1, result.Statistics.TargetFamilies);
    }

    [Fact]
    public void Generate_produces_deterministic_results()
    {
        var generator = new FamilyTargetSetGenerator();
        var request = CreateRequest(
            name: "All Door Families",
            selection: new FamilySelectionCriteria
            {
                Categories = new FamilyCategoryCriteria { CategoryNames = ["Doors"] }
            });

        var first = generator.Generate(request, Provider);
        var second = generator.Generate(request, Provider);

        Assert.Equal(first.TargetSet.TargetSetId, second.TargetSet.TargetSetId);
        Assert.Equal(first.TargetSet.Families![0].Identity.Id, second.TargetSet.Families![0].Identity.Id);
        Assert.Equal(first.Statistics!.TargetFamilies, second.Statistics!.TargetFamilies);
    }

    [Fact]
    public void Generator_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(FamilyTargetSetGenerator).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    private static TargetSetContracts.FamilyTargetSetRequest CreateRequest(
        string name,
        FamilySelectionCriteria? selection = null,
        FamilySelectionCriteria? filtering = null,
        TargetSetContracts.TargetSetComplianceCriteria? compliance = null)
    {
        return new TargetSetContracts.FamilyTargetSetRequest
        {
            Definition = new TargetSetContracts.TargetSetDefinition
            {
                Name = name,
                Description = $"Generated target set for {name}.",
                SelectionCriteria = selection,
                FilteringCriteria = filtering,
                ComplianceCriteria = compliance,
                Metadata = new Dictionary<string, string>
                {
                    ["scenario"] = name
                }
            },
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-target-set-001"
        };
    }

    private static IEnumerable<string> CollectParameterNames(NormalizedFamily family)
    {
        if (family.Parameters is not null)
        {
            foreach (var parameter in family.Parameters)
            {
                yield return parameter.Name;
            }
        }

        if (family.FamilyTypes is null)
        {
            yield break;
        }

        foreach (var familyType in family.FamilyTypes)
        {
            if (familyType.Parameters is null)
            {
                continue;
            }

            foreach (var parameter in familyType.Parameters)
            {
                yield return parameter.Name;
            }
        }
    }
}
