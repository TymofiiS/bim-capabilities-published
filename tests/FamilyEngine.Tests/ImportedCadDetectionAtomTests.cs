using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;
using ImportedCadContracts = BIMCapabilities.Contracts.Engines.Family.ImportedCad;
using BIMCapabilities.Engines.Family.Atoms.ImportedCad;

namespace BIMCapabilities.Engines.Family.Tests;

public class ImportedCadDetectionAtomTests
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Detect_passes_family_without_imported_cad()
    {
        var atom = new ImportedCadDetectionAtom();
        var family = CreateFamily("family-002", "HTL_Window_01", "Windows");
        var result = atom.Detect(CreateRequest(family));

        Assert.Equal(ImportedCadDetectionAtom.DetectionAtomId, result.AtomId);
        Assert.Empty(result.AffectedFamilies!);
        Assert.Single(result.Findings!);
        Assert.False(result.Findings![0].HasImportedCad);
        Assert.Empty(result.Evidence!);
        Assert.Equal(1, result.Statistics!.FamiliesPassed);
        Assert.Equal(0, result.Statistics.FamiliesFailed);
    }

    [Fact]
    public void Detect_fails_family_with_one_imported_cad_reference()
    {
        var atom = new ImportedCadDetectionAtom();
        var family = CreateFamily(
            "family-001",
            "HTL_Door_01",
            "Doors",
            relationships:
            [
                CreateImportedCadRelationship("family-001", "imported-cad-001")
            ]);
        var result = atom.Detect(CreateRequest(family));

        Assert.Single(result.AffectedFamilies!);
        Assert.Equal("HTL_Door_01", result.AffectedFamilies![0].Name);
        Assert.Single(result.Evidence!);
        Assert.Equal(EvidenceCategory.Validation, result.Evidence![0].Category);
        Assert.Equal(EvidenceSeverity.Error, result.Evidence[0].Severity);
        Assert.Equal(1, result.Statistics!.ImportedCadReferencesFound);
    }

    [Fact]
    public void Detect_fails_family_with_multiple_imported_cad_references()
    {
        var atom = new ImportedCadDetectionAtom();
        var family = CreateFamily(
            "family-001",
            "HTL_Door_01",
            "Doors",
            relationships:
            [
                CreateImportedCadRelationship("family-001", "imported-cad-001"),
                CreateImportedCadRelationship("family-001", "imported-cad-002")
            ]);
        var result = atom.Detect(CreateRequest(family));

        Assert.Single(result.AffectedFamilies!);
        Assert.Equal(2, result.Findings![0].ImportedCadRelationships!.Count);
        Assert.Equal(2, result.Evidence!.Count);
        Assert.Equal(2, result.Statistics!.ImportedCadReferencesFound);
    }

    [Fact]
    public void Detect_analyzes_multiple_families()
    {
        var atom = new ImportedCadDetectionAtom();
        var cleanFamily = CreateFamily("family-002", "HTL_Window_01", "Windows");
        var affectedFamily = CreateFamily(
            "family-001",
            "HTL_Door_01",
            "Doors",
            relationships: [CreateImportedCadRelationship("family-001", "imported-cad-001")]);

        var result = atom.Detect(CreateRequest(cleanFamily, affectedFamily));

        Assert.Equal(2, result.Findings!.Count);
        Assert.Single(result.AffectedFamilies!);
        Assert.Equal(1, result.Statistics!.FamiliesPassed);
        Assert.Equal(1, result.Statistics.FamiliesFailed);
    }

    [Fact]
    public void Detect_uses_relationship_query_result_for_imported_cad_references()
    {
        var atom = new ImportedCadDetectionAtom();
        var family = CreateFamily("family-001", "HTL_Door_01", "Doors");
        var queryResult = new RelationshipQueryResult
        {
            Relationships =
            [
                CreateImportedCadRelationship("family-001", "imported-cad-001")
            ]
        };

        var result = atom.Detect(CreateRequest([family], queryResult));

        Assert.Single(result.AffectedFamilies!);
        Assert.Single(result.Evidence!);
    }

    [Fact]
    public void Detect_generates_statistics()
    {
        var atom = new ImportedCadDetectionAtom();
        var family = CreateFamily(
            "family-001",
            "HTL_Door_01",
            "Doors",
            relationships: [CreateImportedCadRelationship("family-001", "imported-cad-001")]);
        var result = atom.Detect(CreateRequest(family));

        Assert.Equal(1, result.Statistics!.FamiliesChecked);
        Assert.Equal(0, result.Statistics.FamiliesPassed);
        Assert.Equal(1, result.Statistics.FamiliesFailed);
        Assert.Equal(1, result.Statistics.ImportedCadReferencesFound);
    }

    [Fact]
    public void Detect_generates_diagnostics()
    {
        var atom = new ImportedCadDetectionAtom();
        var result = atom.Detect(CreateRequest(CreateFamily("family-002", "HTL_Window_01", "Windows")));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ImportedCadDetection.Completed");
        Assert.Equal("Error", result.Metadata!["failureSeverity"]);
    }

    [Fact]
    public void Detect_supports_warning_severity_configuration()
    {
        var atom = new ImportedCadDetectionAtom();
        var family = CreateFamily(
            "family-001",
            "HTL_Door_01",
            "Doors",
            relationships: [CreateImportedCadRelationship("family-001", "imported-cad-001")]);

        var result = atom.Detect(CreateRequest(
            [family],
            relationshipQueryResult: null,
            configuration: new ImportedCadContracts.ImportedCadDetectionConfiguration
            {
                FailureSeverity = EvidenceSeverity.Warning
            }));

        Assert.Equal(EvidenceSeverity.Warning, result.Evidence![0].Severity);
        Assert.Equal("Warning", result.Metadata!["failureSeverity"]);
    }

    [Fact]
    public void Detect_produces_deterministic_results()
    {
        var atom = new ImportedCadDetectionAtom();
        var request = CreateRequest(
            CreateFamily("family-002", "HTL_Window_01", "Windows"),
            CreateFamily(
                "family-001",
                "HTL_Door_01",
                "Doors",
                relationships: [CreateImportedCadRelationship("family-001", "imported-cad-001")]));

        var first = atom.Detect(request);
        var second = atom.Detect(request);

        Assert.Equal(first.Findings![0].Family.Identity.Id, second.Findings![0].Family.Identity.Id);
        Assert.Equal(first.Evidence![0].EvidenceId, second.Evidence![0].EvidenceId);
        Assert.Equal(first.Statistics!.ImportedCadReferencesFound, second.Statistics!.ImportedCadReferencesFound);
    }

    [Fact]
    public void Detection_atom_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(ImportedCadDetectionAtom).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    private static ImportedCadContracts.ImportedCadDetectionRequest CreateRequest(
        params NormalizedFamily[] families)
    {
        return CreateRequest(families, relationshipQueryResult: null);
    }

    private static ImportedCadContracts.ImportedCadDetectionRequest CreateRequest(
        IReadOnlyList<NormalizedFamily> families,
        RelationshipQueryResult? relationshipQueryResult,
        ImportedCadContracts.ImportedCadDetectionConfiguration? configuration = null)
    {
        return new ImportedCadContracts.ImportedCadDetectionRequest
        {
            Families = families,
            RelationshipQueryResult = relationshipQueryResult,
            Configuration = configuration,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-imported-cad-001"
        };
    }

    private static NormalizedFamily CreateFamily(
        string id,
        string name,
        string categoryName,
        IReadOnlyList<NormalizedRelationship>? relationships = null)
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier
            {
                Id = id,
                Kind = "family",
                Scope = "project-document"
            },
            Name = name,
            Category = new NormalizedCategory
            {
                Identifier = new NormalizedIdentifier
                {
                    Id = $"category-{categoryName.ToLowerInvariant()}",
                    Kind = "category"
                },
                Name = categoryName
            },
            Relationships = relationships
        };
    }

    private static NormalizedRelationship CreateImportedCadRelationship(string sourceId, string targetId)
    {
        return new NormalizedRelationship
        {
            Source = new NormalizedIdentifier { Id = sourceId, Kind = "family" },
            Target = new NormalizedIdentifier { Id = targetId, Kind = "importedCad" },
            RelationshipType = NormalizedRelationshipType.Reference,
            Metadata = new Dictionary<string, string>
            {
                ["queryRelationshipType"] = RelationshipType.ImportedCad.ToString(),
                ["referenceType"] = "importedCad"
            }
        };
    }
}
