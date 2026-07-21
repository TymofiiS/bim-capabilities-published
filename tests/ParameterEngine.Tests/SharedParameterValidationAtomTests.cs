using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Parameter;
using EngineSharedParameterFileReference = BIMCapabilities.Contracts.Engines.Parameter.ParameterSharedParameterFileReference;
using BIMCapabilities.Contracts.Evidence;
using SharedParameterContracts = BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using BIMCapabilities.Engines.Parameter.Atoms.SharedParameter;

namespace BIMCapabilities.Engines.Parameter.Tests;

public class SharedParameterValidationAtomTests
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 9, 30, 0, TimeSpan.Zero);

    private const string FireRatingGuid = "f1a2b3c4-d5e6-7890-abcd-ef1234567890";
    private const string AcousticRatingGuid = "a1b2c3d4-e5f6-7890-abcd-ef1234567891";
    private const string RoomNameGuid = "b2c3d4e5-f6a7-8901-bcde-f12345678902";
    private const string ManufacturerGuid = "c3d4e5f6-a7b8-9012-cdef-123456789013";

    [Fact]
    public void Validate_passes_when_shared_parameter_matches_definition()
    {
        var atom = new SharedParameterValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating"]));

        Assert.Equal(SharedParameterValidationAtom.ValidationAtomId, result.AtomId);
        Assert.Single(result.Findings!);
        Assert.True(result.Findings![0].Passed);
        Assert.Equal(SharedParameterContracts.SharedParameterValidationStatus.Valid, result.Findings[0].Status);
        Assert.Empty(result.Evidence!);
        Assert.Equal(1, result.Statistics!.ObjectsPassed);
        Assert.Equal(0, result.Statistics.MissingSharedParameters);
        Assert.Equal(0, result.Statistics.InvalidSharedParameters);
    }

    [Fact]
    public void Validate_detects_missing_shared_parameter()
    {
        var atom = new SharedParameterValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateSharedParameter("RoomName", RoomNameGuid, "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["FireRating"]));

        Assert.Single(result.Findings!);
        Assert.False(result.Findings![0].Passed);
        Assert.Equal(SharedParameterContracts.SharedParameterValidationStatus.Missing, result.Findings[0].Status);
        Assert.Single(result.Evidence!);
        Assert.Equal(EvidenceCategory.Validation, result.Evidence![0].Category);
        Assert.Equal(EvidenceSeverity.Error, result.Evidence[0].Severity);
        Assert.Equal(1, result.Statistics!.MissingSharedParameters);
    }

    [Fact]
    public void Validate_detects_parameter_that_is_not_shared()
    {
        var atom = new SharedParameterValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("FireRating", "60", isShared: false),
                CreateSharedParameter("RoomName", RoomNameGuid, "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["FireRating"]));

        Assert.Single(result.Findings!);
        Assert.Equal(SharedParameterContracts.SharedParameterValidationStatus.NotShared, result.Findings![0].Status);
        Assert.Single(result.Evidence!);
        Assert.Equal(1, result.Statistics!.InvalidSharedParameters);
    }

    [Fact]
    public void Validate_detects_invalid_shared_parameter_definition()
    {
        var atom = new SharedParameterValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateSharedParameter("FireRating", "00000000-0000-0000-0000-000000000000", "60"),
                CreateSharedParameter("RoomName", RoomNameGuid, "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["FireRating"]));

        Assert.Single(result.Findings!);
        Assert.Equal(SharedParameterContracts.SharedParameterValidationStatus.DefinitionMismatch, result.Findings![0].Status);
        Assert.Single(result.Evidence!);
        Assert.Equal(1, result.Statistics!.InvalidSharedParameters);
    }

    [Fact]
    public void Validate_checks_multiple_shared_parameters()
    {
        var atom = new SharedParameterValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating", "RoomName", "Manufacturer"]));

        Assert.Equal(3, result.Findings!.Count);
        Assert.All(result.Findings, finding => Assert.True(finding.Passed));
        Assert.Equal(3, result.Statistics!.SharedParametersChecked);
    }

    [Fact]
    public void Validate_supports_room_name_shared_parameter()
    {
        var atom = new SharedParameterValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["RoomName"]));

        var finding = Assert.Single(result.Findings!);
        Assert.Equal("RoomName", finding.ParameterName);
        Assert.True(finding.Passed);
        Assert.Equal(RoomNameGuid, finding.ExpectedDefinition!.Guid);
    }

    [Fact]
    public void Validate_supports_fire_rating_shared_parameter()
    {
        var atom = new SharedParameterValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating"]));

        var finding = Assert.Single(result.Findings!);
        Assert.Equal("FireRating", finding.ParameterName);
        Assert.True(finding.Passed);
        Assert.Equal(FireRatingGuid, finding.ExpectedDefinition!.Guid);
    }

    [Fact]
    public void Validate_supports_acoustic_rating_shared_parameter()
    {
        var atom = new SharedParameterValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateWindowTargetSet(),
            ["AcousticRating"]));

        var finding = Assert.Single(result.Findings!);
        Assert.Equal("AcousticRating", finding.ParameterName);
        Assert.True(finding.Passed);
        Assert.Equal(AcousticRatingGuid, finding.ExpectedDefinition!.Guid);
    }

    [Fact]
    public void Validate_supports_manufacturer_shared_parameter()
    {
        var atom = new SharedParameterValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["Manufacturer"]));

        var finding = Assert.Single(result.Findings!);
        Assert.Equal("Manufacturer", finding.ParameterName);
        Assert.True(finding.Passed);
        Assert.Equal(ManufacturerGuid, finding.ExpectedDefinition!.Guid);
    }

    [Fact]
    public void Validate_loads_definitions_from_shared_parameter_file()
    {
        var atom = new SharedParameterValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            parameterNamesToValidate: null));

        Assert.Equal(4, result.LoadedDefinitions!.Count);
        Assert.Contains(result.LoadedDefinitions, definition => definition.Name == "FireRating");
        Assert.Contains(result.LoadedDefinitions, definition => definition.Name == "RoomName");
        Assert.Contains(result.LoadedDefinitions, definition => definition.Name == "AcousticRating");
        Assert.Contains(result.LoadedDefinitions, definition => definition.Name == "Manufacturer");
        Assert.Equal(4, result.Findings!.Count);
    }

    [Fact]
    public void Validate_generates_evidence_for_failed_shared_parameters()
    {
        var atom = new SharedParameterValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateSharedParameter("RoomName", RoomNameGuid, "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["FireRating", "RoomName"]));

        Assert.Single(result.Evidence!);
        Assert.Contains("FireRating", result.Evidence![0].Message, StringComparison.Ordinal);
        Assert.Equal("parameter-engine", result.Evidence[0].Source!.EngineId);
    }

    [Fact]
    public void Validate_generates_statistics()
    {
        var atom = new SharedParameterValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateSharedParameter("RoomName", RoomNameGuid, "Lobby"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["FireRating", "RoomName"]));

        Assert.Equal(1, result.Statistics!.ObjectsChecked);
        Assert.Equal(0, result.Statistics.ObjectsPassed);
        Assert.Equal(1, result.Statistics.ObjectsFailed);
        Assert.Equal(2, result.Statistics.SharedParametersChecked);
        Assert.Equal(1, result.Statistics.MissingSharedParameters);
    }

    [Fact]
    public void Validate_generates_diagnostics()
    {
        var atom = new SharedParameterValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating"]));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "SharedParameterValidation.Completed");
        Assert.Equal("target-set-doors-001", result.Metadata!["targetSetId"]);
        Assert.Equal(GetSharedParameterFilePath(), result.Metadata["sharedParameterFilePath"]);
    }

    [Fact]
    public void Validate_produces_deterministic_results()
    {
        var atom = new SharedParameterValidationAtom();
        var request = CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating", "Manufacturer"]);

        var first = atom.Validate(request);
        var second = atom.Validate(request);

        Assert.Equal(first.Findings![0].ParameterName, second.Findings![0].ParameterName);
        Assert.Equal(first.Evidence!.Count, second.Evidence!.Count);
        Assert.Equal(first.Statistics!.MissingSharedParameters, second.Statistics!.MissingSharedParameters);
    }

    [Fact]
    public void Validation_atom_does_not_contain_value_or_correction_methods()
    {
        var atomTypes = new[] { typeof(SharedParameterValidationAtom) };

        Assert.All(atomTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Value", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Validation_atom_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(SharedParameterValidationAtom).Assembly;
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
                CreateSharedParameter("RoomName", RoomNameGuid, "Lobby"),
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
                CreateSharedParameter("RoomName", RoomNameGuid, "Office"),
                CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
            ]
        };
    }

    private static SharedParameterContracts.SharedParameterValidationRequest CreateRequest(
        ParameterTargetSet targetSet,
        IReadOnlyList<string>? parameterNamesToValidate = null,
        ParameterQueryResult? parameterQueryResult = null)
    {
        return new SharedParameterContracts.SharedParameterValidationRequest
        {
            TargetSet = targetSet,
            ParameterQueryResult = parameterQueryResult,
            SharedParameterFile = new EngineSharedParameterFileReference
            {
                FilePath = GetSharedParameterFilePath()
            },
            ParameterNamesToValidate = parameterNamesToValidate,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-shared-parameter-001"
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
