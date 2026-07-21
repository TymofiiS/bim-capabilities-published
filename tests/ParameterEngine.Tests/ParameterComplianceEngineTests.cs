using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Evidence;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using ValueContracts = BIMCapabilities.Contracts.Engines.Parameter.Value;
using EngineSharedParameterFileReference = BIMCapabilities.Contracts.Engines.Parameter.ParameterSharedParameterFileReference;
using BIMCapabilities.Engines.Parameter.Compliance;

namespace BIMCapabilities.Engines.Parameter.Tests;

public class ParameterComplianceEngineTests
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 10, 30, 0, TimeSpan.Zero);

    private const string FireRatingGuid = "f1a2b3c4-d5e6-7890-abcd-ef1234567890";
    private const string AcousticRatingGuid = "a1b2c3d4-e5f6-7890-abcd-ef1234567891";
    private const string ManufacturerGuid = "c3d4e5f6-a7b8-9012-cdef-123456789013";

    [Fact]
    public void Evaluate_runs_existence_only_workflow()
    {
        var engine = new ParameterComplianceEngine();
        var result = engine.Evaluate(CreateRequest(
            CreateDoorTargetSet(),
            requiredParameterNames: ["FireRating", "RoomName"],
            sharedParameterNames: null,
            valueRules: null));

        Assert.NotNull(result.ExistenceResult);
        Assert.Null(result.SharedParameterResult);
        Assert.Null(result.ValueResult);
        Assert.Equal(2, result.Findings!.Count);
        Assert.All(result.Findings, finding => Assert.Equal("existence", finding.ValidationStage));
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterExistence.Completed");
    }

    [Fact]
    public void Evaluate_runs_shared_parameter_only_workflow()
    {
        var engine = new ParameterComplianceEngine();
        var result = engine.Evaluate(CreateRequest(
            CreateDoorTargetSet(),
            requiredParameterNames: null,
            sharedParameterNames: ["FireRating", "Manufacturer"],
            valueRules: null));

        Assert.Null(result.ExistenceResult);
        Assert.NotNull(result.SharedParameterResult);
        Assert.Null(result.ValueResult);
        Assert.Equal(2, result.Findings!.Count);
        Assert.All(result.Findings, finding => Assert.Equal("shared-parameter", finding.ValidationStage));
    }

    [Fact]
    public void Evaluate_runs_value_validation_only_workflow()
    {
        var engine = new ParameterComplianceEngine();
        var result = engine.Evaluate(CreateRequest(
            CreateDoorTargetSet(),
            requiredParameterNames: null,
            sharedParameterNames: null,
            valueRules: [RequiredRule("FireRating"), RequiredRule("RoomName")]));

        Assert.Null(result.ExistenceResult);
        Assert.Null(result.SharedParameterResult);
        Assert.NotNull(result.ValueResult);
        Assert.Equal(2, result.Findings!.Count);
        Assert.All(result.Findings, finding => Assert.Equal("value", finding.ValidationStage));
    }

    [Fact]
    public void Evaluate_runs_combined_workflow()
    {
        var engine = new ParameterComplianceEngine();
        var result = engine.Evaluate(CreateDoorComplianceRequest(CreateDoorTargetSet()));

        Assert.NotNull(result.ExistenceResult);
        Assert.NotNull(result.SharedParameterResult);
        Assert.NotNull(result.ValueResult);
        Assert.Equal(8, result.Findings!.Count);
        Assert.Equal(3, result.Statistics!.ExistenceChecksRun);
        Assert.Equal(2, result.Statistics.SharedParameterChecksRun);
        Assert.Equal(3, result.Statistics.ValueChecksRun);
    }

    [Fact]
    public void Evaluate_supports_door_family_mvp_scenario()
    {
        var engine = new ParameterComplianceEngine();
        var result = engine.Evaluate(CreateDoorComplianceRequest(CreateDoorTargetSet()));

        Assert.Equal(ParameterComplianceEngine.ComplianceEngineId, result.EngineId);
        Assert.Equal(100m, result.Summary!.CompliancePercentage);
        Assert.Equal(8, result.Summary.PassedChecks);
        Assert.Equal(0, result.Summary.FailedChecks);
        Assert.Empty(result.Evidence!);
        Assert.Contains(result.Findings!, finding => finding.ParameterName == "FireRating" && finding.Passed);
        Assert.Contains(result.Findings!, finding => finding.ParameterName == "RoomName" && finding.Passed);
        Assert.Contains(result.Findings!, finding => finding.ParameterName == "Manufacturer" && finding.Passed);
    }

    [Fact]
    public void Evaluate_supports_window_family_mvp_scenario()
    {
        var engine = new ParameterComplianceEngine();
        var result = engine.Evaluate(CreateWindowComplianceRequest(CreateWindowTargetSet()));

        Assert.Equal(100m, result.Summary!.CompliancePercentage);
        Assert.Contains(result.Findings!, finding => finding.ParameterName == "AcousticRating" && finding.Passed);
        Assert.Contains(result.Findings!, finding => finding.ParameterName == "RoomName" && finding.Passed);
        Assert.Contains(result.Findings!, finding => finding.ParameterName == "Manufacturer" && finding.Passed);
    }

    [Fact]
    public void Evaluate_aggregates_evidence_from_all_atoms()
    {
        var engine = new ParameterComplianceEngine();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("RoomName", "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };

        var result = engine.Evaluate(CreateDoorComplianceRequest(targetSet));

        Assert.NotEmpty(result.Evidence!);
        Assert.Contains(result.Evidence!, record => record.EvidenceId.Contains("parameter-missing", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Evidence!, record => record.EvidenceId.Contains("shared-parameter-missing", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Evidence!, record => record.EvidenceId.Contains("parameter-value-missingvalue", StringComparison.Ordinal));
        Assert.Equal(EvidenceSeverity.Error, result.Evidence!.First().Severity);
    }

    [Fact]
    public void Evaluate_aggregates_diagnostics_from_all_atoms()
    {
        var engine = new ParameterComplianceEngine();
        var result = engine.Evaluate(CreateDoorComplianceRequest(CreateDoorTargetSet()));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterExistence.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "SharedParameterValidation.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterValueValidation.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterCompliance.Completed");
    }

    [Fact]
    public void Evaluate_aggregates_statistics_from_all_atoms()
    {
        var engine = new ParameterComplianceEngine();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("RoomName", "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };

        var result = engine.Evaluate(CreateDoorComplianceRequest(targetSet));

        Assert.Equal(1, result.Statistics!.ObjectsChecked);
        Assert.Equal(1, result.Statistics.ObjectsFailed);
        Assert.Equal(8, result.Statistics.ParametersChecked);
        Assert.Equal(1, result.Statistics.MissingParameters);
        Assert.Equal(1, result.Statistics.MissingSharedParameters);
        Assert.Equal(1, result.Statistics.MissingValues);
    }

    [Fact]
    public void Evaluate_generates_compliance_summary()
    {
        var engine = new ParameterComplianceEngine();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("RoomName", "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };

        var result = engine.Evaluate(CreateDoorComplianceRequest(targetSet));

        Assert.Equal(1, result.Summary!.ObjectsChecked);
        Assert.Equal(8, result.Summary.ParametersChecked);
        Assert.Equal(5, result.Summary.PassedChecks);
        Assert.Equal(3, result.Summary.FailedChecks);
        Assert.Equal(62.50m, result.Summary.CompliancePercentage);
    }

    [Fact]
    public void Evaluate_produces_deterministic_results()
    {
        var engine = new ParameterComplianceEngine();
        var request = CreateDoorComplianceRequest(CreateDoorTargetSet());

        var first = engine.Evaluate(request);
        var second = engine.Evaluate(request);

        Assert.Equal(first.Summary!.CompliancePercentage, second.Summary!.CompliancePercentage);
        Assert.Equal(first.Evidence!.Count, second.Evidence!.Count);
        Assert.Equal(first.Findings![0].ParameterName, second.Findings![0].ParameterName);
    }

    [Fact]
    public void Compliance_engine_does_not_contain_correction_or_transaction_methods()
    {
        var engineTypes = new[] { typeof(ParameterComplianceEngine) };

        Assert.All(engineTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Transaction", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Create", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Compliance_engine_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(ParameterComplianceEngine).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    private static string GetSharedParameterFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "CompanySharedParameters.txt");
    }

    private static ComplianceContracts.ParameterComplianceRequest CreateDoorComplianceRequest(ParameterTargetSet targetSet)
    {
        return CreateRequest(
            targetSet,
            requiredParameterNames: ["FireRating", "RoomName", "Manufacturer"],
            sharedParameterNames: ["FireRating", "Manufacturer"],
            valueRules:
            [
                RequiredRule("FireRating"),
                RequiredRule("RoomName"),
                RequiredRule("Manufacturer")
            ]);
    }

    private static ComplianceContracts.ParameterComplianceRequest CreateWindowComplianceRequest(ParameterTargetSet targetSet)
    {
        return CreateRequest(
            targetSet,
            requiredParameterNames: ["AcousticRating", "RoomName", "Manufacturer"],
            sharedParameterNames: ["AcousticRating", "Manufacturer"],
            valueRules:
            [
                RequiredRule("AcousticRating"),
                RequiredRule("RoomName"),
                RequiredRule("Manufacturer")
            ]);
    }

    private static ComplianceContracts.ParameterComplianceRequest CreateRequest(
        ParameterTargetSet targetSet,
        IReadOnlyList<string>? requiredParameterNames,
        IReadOnlyList<string>? sharedParameterNames,
        IReadOnlyList<ValueContracts.ParameterValueRule>? valueRules)
    {
        return new ComplianceContracts.ParameterComplianceRequest
        {
            TargetSet = targetSet,
            SharedParameterFile = sharedParameterNames is { Count: > 0 }
                ? new EngineSharedParameterFileReference { FilePath = GetSharedParameterFilePath() }
                : null,
            RequiredParameterNames = requiredParameterNames,
            SharedParameterNamesToValidate = sharedParameterNames,
            ValueRules = valueRules,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-compliance-001"
        };
    }

    private static ValueContracts.ParameterValueRule RequiredRule(string parameterName)
    {
        return new ValueContracts.ParameterValueRule
        {
            ParameterName = parameterName,
            RequiredValue = true
        };
    }

    private static ParameterTargetSet CreateDoorTargetSet()
    {
        return new ParameterTargetSet
        {
            TargetSetId = "target-set-doors-001",
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "HTL_Door_01_900x2100")
            ],
            TargetParameters =
            [
                CreateSharedParameter("FireRating", FireRatingGuid, "60"),
                CreateParameter("RoomName", "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };
    }

    private static ParameterTargetSet CreateWindowTargetSet()
    {
        return new ParameterTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetTypes =
            [
                CreateFamilyType("family-type-003", "HTL_Window_01_1200x1200")
            ],
            TargetParameters =
            [
                CreateSharedParameter("AcousticRating", AcousticRatingGuid, "45"),
                CreateParameter("RoomName", "Office"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };
    }

    private static NormalizedFamilyType CreateFamilyType(
        string id,
        string name,
        IReadOnlyList<NormalizedParameter>? parameters = null)
    {
        return new NormalizedFamilyType
        {
            Identity = new NormalizedIdentifier { Id = id, Kind = "familyType" },
            Name = name,
            Parameters = parameters
        };
    }

    private static NormalizedParameter CreateSharedParameter(string name, string guid, string? value = null)
    {
        return CreateParameter(name, value, isShared: true, guid: guid);
    }

    private static NormalizedParameter CreateParameter(
        string name,
        string? value = null,
        bool isShared = false,
        string? guid = null)
    {
        var metadata = guid is null
            ? null
            : new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["sharedParameterGuid"] = guid
            };

        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier
            {
                Id = guid ?? $"parameter-{name.ToLowerInvariant()}",
                Kind = "parameter"
            },
            Name = name,
            Value = value,
            StorageType = NormalizedParameterStorageType.String,
            IsSharedParameter = isShared,
            Metadata = metadata
        };
    }
}
