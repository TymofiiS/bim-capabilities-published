using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Evidence;
using ValueContracts = BIMCapabilities.Contracts.Engines.Parameter.Value;
using BIMCapabilities.Engines.Parameter.Atoms.Value;

namespace BIMCapabilities.Engines.Parameter.Tests;

public class ParameterValueValidationAtomTests
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_passes_when_required_value_is_present()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [RequiredRule("FireRating")]));

        Assert.Equal(ParameterValueValidationAtom.ValidationAtomId, result.AtomId);
        Assert.Single(result.Findings!);
        Assert.True(result.Findings![0].Passed);
        Assert.Empty(result.Evidence!);
        Assert.Equal(1, result.Statistics!.ObjectsPassed);
        Assert.Equal(0, result.Statistics.MissingValues);
    }

    [Fact]
    public void Validate_detects_missing_required_value()
    {
        var atom = new ParameterValueValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("FireRating", "   "),
                CreateParameter("RoomName", "Lobby"),
                CreateParameter("Manufacturer", "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, [RequiredRule("FireRating")]));

        Assert.Single(result.Findings!);
        Assert.False(result.Findings![0].Passed);
        Assert.Equal(ValueContracts.ParameterValueValidationStatus.MissingValue, result.Findings[0].Status);
        Assert.Single(result.Evidence!);
        Assert.Equal(EvidenceSeverity.Error, result.Evidence![0].Severity);
        Assert.Equal(1, result.Statistics!.MissingValues);
    }

    [Fact]
    public void Validate_passes_when_allowed_value_matches()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [new ValueContracts.ParameterValueRule
            {
                ParameterName = "FireRating",
                AllowedValues = ["30", "60", "90"]
            }]));

        Assert.Single(result.Findings!);
        Assert.True(result.Findings![0].Passed);
    }

    [Fact]
    public void Validate_detects_invalid_allowed_value()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [new ValueContracts.ParameterValueRule
            {
                ParameterName = "FireRating",
                AllowedValues = ["30", "90"]
            }]));

        Assert.Single(result.Findings!);
        Assert.Equal(ValueContracts.ParameterValueValidationStatus.InvalidValue, result.Findings![0].Status);
        Assert.Equal(1, result.Statistics!.InvalidValues);
    }

    [Fact]
    public void Validate_detects_forbidden_value()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [new ValueContracts.ParameterValueRule
            {
                ParameterName = "Manufacturer",
                ForbiddenValues = ["HTL Components", "Unknown Vendor"]
            }]));

        Assert.Single(result.Findings!);
        Assert.Equal(ValueContracts.ParameterValueValidationStatus.InvalidValue, result.Findings![0].Status);
        Assert.Contains("forbidden", result.Findings[0].ViolationReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_enforces_minimum_length()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [new ValueContracts.ParameterValueRule
            {
                ParameterName = "RoomName",
                MinimumLength = 6
            }]));

        Assert.Single(result.Findings!);
        Assert.Equal(ValueContracts.ParameterValueValidationStatus.InvalidValue, result.Findings![0].Status);
    }

    [Fact]
    public void Validate_enforces_maximum_length()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [new ValueContracts.ParameterValueRule
            {
                ParameterName = "RoomName",
                MaximumLength = 3
            }]));

        Assert.Single(result.Findings!);
        Assert.Equal(ValueContracts.ParameterValueValidationStatus.InvalidValue, result.Findings![0].Status);
    }

    [Fact]
    public void Validate_enforces_regular_expression()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [new ValueContracts.ParameterValueRule
            {
                ParameterName = "FireRating",
                RegularExpression = @"^\d+$"
            }]));

        Assert.Single(result.Findings!);
        Assert.True(result.Findings![0].Passed);

        var invalidTargetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("FireRating", "60min"),
                CreateParameter("RoomName", "Lobby"),
                CreateParameter("Manufacturer", "HTL Components")
            ]
        };

        var invalidResult = atom.Validate(CreateRequest(invalidTargetSet, [new ValueContracts.ParameterValueRule
        {
            ParameterName = "FireRating",
            RegularExpression = @"^\d+$"
        }]));

        Assert.Equal(ValueContracts.ParameterValueValidationStatus.InvalidValue, invalidResult.Findings![0].Status);
    }

    [Fact]
    public void Validate_supports_room_name_not_empty()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [RequiredRule("RoomName")]));

        Assert.True(result.Findings!.Single(finding => finding.ParameterName == "RoomName").Passed);
    }

    [Fact]
    public void Validate_supports_fire_rating_not_empty()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [RequiredRule("FireRating")]));

        Assert.True(result.Findings!.Single(finding => finding.ParameterName == "FireRating").Passed);
    }

    [Fact]
    public void Validate_supports_acoustic_rating_not_empty()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateWindowTargetSet(),
            [RequiredRule("AcousticRating")]));

        Assert.True(result.Findings!.Single(finding => finding.ParameterName == "AcousticRating").Passed);
    }

    [Fact]
    public void Validate_supports_manufacturer_not_empty()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [RequiredRule("Manufacturer")]));

        Assert.True(result.Findings!.Single(finding => finding.ParameterName == "Manufacturer").Passed);
    }

    [Fact]
    public void Validate_uses_rule_severity_for_evidence()
    {
        var atom = new ParameterValueValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("FireRating", string.Empty),
                CreateParameter("RoomName", "Lobby"),
                CreateParameter("Manufacturer", "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, [new ValueContracts.ParameterValueRule
        {
            ParameterName = "FireRating",
            RequiredValue = true,
            Severity = EvidenceSeverity.Warning
        }]));

        Assert.Equal(EvidenceSeverity.Warning, result.Evidence!.Single().Severity);
    }

    [Fact]
    public void Validate_includes_custom_rule_identifier_in_evidence()
    {
        var atom = new ParameterValueValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("FireRating", string.Empty),
                CreateParameter("RoomName", "Lobby"),
                CreateParameter("Manufacturer", "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, [new ValueContracts.ParameterValueRule
        {
            ParameterName = "FireRating",
            RequiredValue = true,
            CustomRuleIdentifier = "client.fire-rating.required"
        }]));

        Assert.Equal(
            "client.fire-rating.required",
            result.Evidence!.Single().StructuredData!["customRuleIdentifier"]);
    }

    [Fact]
    public void Validate_generates_statistics()
    {
        var atom = new ParameterValueValidationAtom();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetParameters =
            [
                CreateParameter("FireRating", string.Empty),
                CreateParameter("RoomName", "Lobby"),
                CreateParameter("Manufacturer", "HTL Components")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, [RequiredRule("FireRating"), RequiredRule("RoomName")]));

        Assert.Equal(1, result.Statistics!.ObjectsChecked);
        Assert.Equal(0, result.Statistics.ObjectsPassed);
        Assert.Equal(1, result.Statistics.ObjectsFailed);
        Assert.Equal(2, result.Statistics.ParametersChecked);
        Assert.Equal(1, result.Statistics.MissingValues);
    }

    [Fact]
    public void Validate_generates_diagnostics()
    {
        var atom = new ParameterValueValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateDoorTargetSet(),
            [RequiredRule("FireRating")]));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterValueValidation.Completed");
        Assert.Equal("target-set-doors-001", result.Metadata!["targetSetId"]);
    }

    [Fact]
    public void Validate_produces_deterministic_results()
    {
        var atom = new ParameterValueValidationAtom();
        var request = CreateRequest(
            CreateDoorTargetSet(),
            [RequiredRule("FireRating"), RequiredRule("Manufacturer")]);

        var first = atom.Validate(request);
        var second = atom.Validate(request);

        Assert.Equal(first.Findings![0].ParameterName, second.Findings![0].ParameterName);
        Assert.Equal(first.Evidence!.Count, second.Evidence!.Count);
        Assert.Equal(first.Statistics!.MissingValues, second.Statistics!.MissingValues);
    }

    [Fact]
    public void Validation_atom_does_not_contain_correction_or_creation_methods()
    {
        var atomTypes = new[] { typeof(ParameterValueValidationAtom) };

        Assert.All(atomTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Create", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Validation_atom_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(ParameterValueValidationAtom).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
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
    public void Validate_checks_instance_bound_parameters_on_each_placed_instance()
    {
        var atom = new ParameterValueValidationAtom();
        var targetSet = new ParameterTargetSet
        {
            TargetSetId = "target-set-windows-instances",
            TargetInstances =
            [
                new NormalizedPlacedInstance
                {
                    Identity = new NormalizedIdentifier { Id = "instance-001", Kind = "familyInstance" },
                    Name = "Window 101",
                    FamilyName = "M_Window-Fixed",
                    FamilyTypeName = "900 x 1200mm",
                    CategoryName = "Windows",
                    Parameters = [CreateParameter("MY_Room", "TBD")]
                },
                new NormalizedPlacedInstance
                {
                    Identity = new NormalizedIdentifier { Id = "instance-002", Kind = "familyInstance" },
                    Name = "Window 102",
                    FamilyName = "M_Window-Fixed",
                    FamilyTypeName = "900 x 1200mm",
                    CategoryName = "Windows",
                    Parameters = [CreateParameter("MY_Room", "")]
                }
            ]
        };

        var result = atom.Validate(CreateRequest(
            targetSet,
            [RequiredRule("MY_Room")],
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["MY_Room"] = true
            }));

        Assert.Equal(2, result.Findings!.Count);
        Assert.True(result.Findings!.Single(finding => finding.ObjectId == "instance-001").Passed);
        Assert.False(result.Findings.Single(finding => finding.ObjectId == "instance-002").Passed);
        Assert.Single(result.Evidence!);
    }

    [Fact]
    public void Validate_skips_instance_value_checks_when_parameter_is_not_present()
    {
        var atom = new ParameterValueValidationAtom();
        var targetSet = new ParameterTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetInstances =
            [
                new NormalizedPlacedInstance
                {
                    Identity = new NormalizedIdentifier { Id = "instance-001", Kind = "familyInstance" },
                    Name = "Window 101",
                    FamilyName = "M_Window-Fixed",
                    FamilyTypeName = "900 x 1200mm",
                    CategoryName = "Windows",
                    Parameters = []
                }
            ]
        };

        var result = atom.Validate(CreateRequest(
            targetSet,
            [RequiredRule("MY_Room")],
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["MY_Room"] = true
            }));

        Assert.Empty(result.Findings!);
        Assert.Empty(result.Evidence!);
    }

    private static ValueContracts.ParameterValueValidationRequest CreateRequest(
        ParameterTargetSet targetSet,
        IReadOnlyList<ValueContracts.ParameterValueRule> rules,
        IReadOnlyDictionary<string, bool>? parameterBindings = null,
        ParameterQueryResult? parameterQueryResult = null)
    {
        return new ValueContracts.ParameterValueValidationRequest
        {
            TargetSet = targetSet,
            ParameterQueryResult = parameterQueryResult,
            Rules = rules,
            ParameterBindings = parameterBindings,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-value-001"
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
