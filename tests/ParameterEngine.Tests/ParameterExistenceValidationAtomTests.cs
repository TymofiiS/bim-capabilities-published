using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Evidence;
using ExistenceContracts = BIMCapabilities.Contracts.Engines.Parameter.Existence;
using BIMCapabilities.Engines.Parameter.Atoms.Existence;

namespace BIMCapabilities.Engines.Parameter.Tests;

public class ParameterExistenceValidationAtomTests
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_passes_when_required_parameter_exists()
    {
        var atom = new ParameterExistenceValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating"]));

        Assert.Equal(ParameterExistenceValidationAtom.ValidationAtomId, result.AtomId);
        Assert.All(result.Findings!, finding => Assert.True(finding.Exists));
        Assert.Empty(result.Evidence!);
        Assert.Equal(1, result.Statistics!.ObjectsPassed);
        Assert.Equal(0, result.Statistics.MissingParameters);
    }

    [Fact]
    public void Validate_detects_missing_required_parameter()
    {
        var atom = new ParameterExistenceValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("RoomName", "Lobby"),
                CreateParameter("Manufacturer", "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["FireRating"]));

        Assert.Single(result.Findings!);
        Assert.False(result.Findings![0].Exists);
        Assert.Single(result.Evidence!);
        Assert.Equal(EvidenceCategory.Validation, result.Evidence![0].Category);
        Assert.Equal(EvidenceSeverity.Error, result.Evidence[0].Severity);
        Assert.Equal(1, result.Statistics!.MissingParameters);
    }

    [Fact]
    public void Validate_checks_multiple_required_parameters()
    {
        var atom = new ParameterExistenceValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating", "RoomName", "Manufacturer"]));

        Assert.Equal(3, result.Findings!.Count);
        Assert.Equal(3, result.Findings.Count(finding => finding.Exists));
    }

    [Fact]
    public void Validate_checks_multiple_family_types()
    {
        var atom = new ParameterExistenceValidationAtom();
        var targetSet = new ParameterTargetSet
        {
            TargetSetId = "target-set-doors-001",
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "HTL_Door_01_900x2100", [CreateParameter("FireRating", "60")]),
                CreateFamilyType("family-type-002", "HTL_Door_01_1000x2100", [])
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["FireRating"]));

        Assert.Equal(2, result.Findings!.Count);
        Assert.True(result.Findings!.Single(finding => finding.ObjectId == "family-type-001").Exists);
        Assert.False(result.Findings.Single(finding => finding.ObjectId == "family-type-002").Exists);
        Assert.Equal(1, result.Statistics!.ObjectsPassed);
        Assert.Equal(1, result.Statistics.ObjectsFailed);
    }

    [Fact]
    public void Validate_uses_parameter_query_result_for_existence_checks()
    {
        var atom = new ParameterExistenceValidationAtom();
        var targetSet = new ParameterTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetTypes =
            [
                CreateFamilyType("family-type-003", "HTL_Window_01_1200x1200")
            ]
        };
        var queryResult = new ParameterQueryResult
        {
            Parameters = [CreateParameter("AcousticRating", "45")]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["AcousticRating"], queryResult));

        Assert.Single(result.Findings!);
        Assert.True(result.Findings![0].Exists);
    }

    [Fact]
    public void Validate_supports_mvp_window_parameters()
    {
        var atom = new ParameterExistenceValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateWindowTargetSet(),
            ["AcousticRating", "RoomName", "Manufacturer"]));

        Assert.Equal(3, result.Findings!.Count);
        Assert.All(result.Findings, finding => Assert.True(finding.Exists));
    }

    [Fact]
    public void Validate_generates_statistics()
    {
        var atom = new ParameterExistenceValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters = [CreateParameter("RoomName", "Lobby")]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["FireRating", "RoomName"]));

        Assert.Equal(1, result.Statistics!.ObjectsChecked);
        Assert.Equal(0, result.Statistics.ObjectsPassed);
        Assert.Equal(1, result.Statistics.ObjectsFailed);
        Assert.Equal(1, result.Statistics.MissingParameters);
    }

    [Fact]
    public void Validate_generates_diagnostics()
    {
        var atom = new ParameterExistenceValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating"]));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterExistence.Completed");
        Assert.Equal("target-set-doors-001", result.Metadata!["targetSetId"]);
    }

    [Fact]
    public void Validate_produces_deterministic_results()
    {
        var atom = new ParameterExistenceValidationAtom();
        var request = CreateRequest(
            CreateDoorTargetSet(),
            ["FireRating", "Manufacturer"]);

        var first = atom.Validate(request);
        var second = atom.Validate(request);

        Assert.Equal(first.Findings![0].ParameterName, second.Findings![0].ParameterName);
        Assert.Equal(first.Evidence!.Count, second.Evidence!.Count);
        Assert.Equal(first.Statistics!.MissingParameters, second.Statistics!.MissingParameters);
    }

    [Fact]
    public void Validation_atom_does_not_contain_value_shared_or_correction_methods()
    {
        var atomTypes = new[] { typeof(ParameterExistenceValidationAtom) };

        Assert.All(atomTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("SharedParameter", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Value", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Validation_atom_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(ParameterExistenceValidationAtom).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
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
                CreateParameter("FireRating", "60"),
                CreateParameter("RoomName", "Lobby"),
                CreateParameter("Manufacturer", "HTL Components")
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
                CreateParameter("AcousticRating", "45"),
                CreateParameter("RoomName", "Office"),
                CreateParameter("Manufacturer", "HTL Components")
            ]
        };
    }

    [Fact]
    public void Validate_checks_instance_bound_parameters_at_family_level()
    {
        var atom = new ParameterExistenceValidationAtom();
        var targetSet = new ParameterTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetFamilies =
            [
                new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier { Id = "family-window-001", Kind = "family" },
                    Name = "M_Window-Fixed",
                    Category = new NormalizedCategory
                    {
                        Identifier = new NormalizedIdentifier { Id = "category-windows", Kind = "category" },
                        Name = "Windows"
                    },
                    Parameters = [CreateParameter("MY_Room", "TBD")],
                    FamilyTypes =
                    [
                        CreateFamilyType("family-type-001", "900 x 1200mm"),
                        CreateFamilyType("family-type-002", "600 x 900mm")
                    ]
                },
                new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier { Id = "family-window-002", Kind = "family" },
                    Name = "M_Window-Double-Hung",
                    Category = new NormalizedCategory
                    {
                        Identifier = new NormalizedIdentifier { Id = "category-windows", Kind = "category" },
                        Name = "Windows"
                    },
                    FamilyTypes = [CreateFamilyType("family-type-003", "700 x 1200mm")]
                }
            ],
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "900 x 1200mm"),
                CreateFamilyType("family-type-002", "600 x 900mm"),
                CreateFamilyType("family-type-003", "700 x 1200mm")
            ]
        };

        var result = atom.Validate(CreateRequest(
            targetSet,
            ["MY_Room"],
            parameterBindings: new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["MY_Room"] = true
            }));

        Assert.Equal(2, result.Findings!.Count);
        Assert.True(result.Findings!.Single(finding => finding.ObjectId == "family-window-001").Exists);
        Assert.False(result.Findings.Single(finding => finding.ObjectId == "family-window-002").Exists);
        Assert.Single(result.Evidence!);
        Assert.Equal("family", result.Findings[0].ObjectKind);
    }

    private static ExistenceContracts.ParameterExistenceRequest CreateRequest(
        ParameterTargetSet targetSet,
        IReadOnlyList<string> requiredParameterNames,
        ParameterQueryResult? parameterQueryResult = null,
        IReadOnlyDictionary<string, bool>? parameterBindings = null)
    {
        return new ExistenceContracts.ParameterExistenceRequest
        {
            TargetSet = targetSet,
            ParameterQueryResult = parameterQueryResult,
            RequiredParameterNames = requiredParameterNames,
            ParameterBindings = parameterBindings,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-existence-001"
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

    private static NormalizedParameter CreateParameter(string name, string? value = null)
    {
        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier
            {
                Id = $"parameter-{name.ToLowerInvariant()}",
                Kind = "parameter"
            },
            Name = name,
            Value = value,
            StorageType = NormalizedParameterStorageType.String
        };
    }
}
