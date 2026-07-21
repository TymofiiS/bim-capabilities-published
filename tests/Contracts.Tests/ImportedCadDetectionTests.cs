using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Family;
using ImportedCadContracts = BIMCapabilities.Contracts.Engines.Family.ImportedCad;

namespace BIMCapabilities.Contracts.Tests;

public class ImportedCadDetectionTests
{
    [Fact]
    public void Imported_cad_detection_contracts_are_data_only_types()
    {
        var detectionTypes = typeof(ImportedCadContracts.ImportedCadDetectionRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ImportedCadContracts.ImportedCadDetectionRequest).Namespace);

        Assert.All(detectionTypes, type =>
        {
            if (type == typeof(ImportedCadContracts.IImportedCadDetectionAtom))
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
    public void ImportedCadDetectionRequest_and_result_can_be_constructed()
    {
        var request = new ImportedCadContracts.ImportedCadDetectionRequest
        {
            Families = [],
            Configuration = new ImportedCadContracts.ImportedCadDetectionConfiguration
            {
                FailureSeverity = EvidenceSeverity.Warning
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-imported-cad-001"
        };

        var result = new ImportedCadContracts.ImportedCadDetectionResult
        {
            AtomId = "family.detection.imported-cad",
            Findings = [],
            Evidence = [],
            Statistics = new ImportedCadContracts.ImportedCadDetectionStatistics
            {
                FamiliesChecked = 0,
                FamiliesPassed = 0,
                FamiliesFailed = 0,
                ImportedCadReferencesFound = 0
            },
            Diagnostics =
            [
                new FamilyEngineDiagnostic
                {
                    Code = "ImportedCadDetection.Completed",
                    Message = "Detection completed.",
                    Severity = FamilyEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal(EvidenceSeverity.Warning, request.Configuration!.FailureSeverity);
        Assert.Equal("family.detection.imported-cad", result.AtomId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void ImportedCadDetectionRequest_supports_json_round_trip_serialization()
    {
        var original = new ImportedCadContracts.ImportedCadDetectionRequest
        {
            Families =
            [
                new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier { Id = "family-001", Kind = "family" },
                    Name = "HTL_Door_01"
                }
            ],
            RelationshipQueryResult = new RelationshipQueryResult
            {
                Relationships = []
            },
            Configuration = new ImportedCadContracts.ImportedCadDetectionConfiguration
            {
                FailureSeverity = EvidenceSeverity.Error
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-imported-cad-001"
        };

        var json = JsonSerializer.Serialize(original, FamilyEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<ImportedCadContracts.ImportedCadDetectionRequest>(json, FamilyEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RuleId, roundTrip.RuleId);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(original.Configuration!.FailureSeverity, roundTrip.Configuration!.FailureSeverity);
    }

    [Fact]
    public void IImportedCadDetectionAtom_defines_detection_contract()
    {
        var method = Assert.Single(typeof(ImportedCadContracts.IImportedCadDetectionAtom).GetMethods(), candidate => candidate.Name == "Detect");

        Assert.Equal(typeof(ImportedCadContracts.ImportedCadDetectionResult), method.ReturnType);
        Assert.Equal(typeof(ImportedCadContracts.ImportedCadDetectionRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Imported_cad_detection_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ImportedCadContracts.ImportedCadDetectionRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
