using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Contracts.Evidence;
using PatternContracts = BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Engines.Naming.Atoms.Pattern;

namespace BIMCapabilities.Engines.Naming.Tests;

public class NamingPatternValidationAtomTests
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 11, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_passes_for_valid_dr_single_door_pattern()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_SingleDoor"),
            CreateDoorPatternRule()));

        Assert.Equal(NamingPatternValidationAtom.ValidationAtomId, result.AtomId);
        Assert.True(result.Findings!.Single().Passed);
        Assert.Equal(PatternContracts.NamingPatternValidationStatus.Valid, result.Findings!.Single().Status);
        Assert.Empty(result.Evidence!);
    }

    [Fact]
    public void Validate_passes_for_valid_dr_double_door_pattern()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_DoubleDoor"),
            CreateDoorPatternRule()));

        Assert.True(result.Findings!.Single().Passed);
    }

    [Fact]
    public void Validate_detects_invalid_dr_dash_pattern()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR-"),
            CreateDoorPatternRule()));

        Assert.False(result.Findings!.Single().Passed);
        Assert.Equal(PatternContracts.NamingPatternValidationStatus.ForbiddenCharacter, result.Findings!.Single().Status);
        Assert.Equal(1, result.Statistics!.InvalidCharacterViolations);
    }

    [Fact]
    public void Validate_detects_invalid_dr_space_pattern()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR Door"),
            CreateDoorPatternRule()));

        Assert.Equal(PatternContracts.NamingPatternValidationStatus.ForbiddenCharacter, result.Findings!.Single().Status);
        Assert.Equal(1, result.Statistics!.InvalidCharacterViolations);
    }

    [Fact]
    public void Validate_supports_configurable_dr_01_pattern()
    {
        var atom = new NamingPatternValidationAtom();
        var restrictiveRule = new PatternContracts.NamingPatternRule
        {
            TokenizedPattern = "DR_{Token}",
            AllowNumericTokenStart = false
        };
        var permissiveRule = new PatternContracts.NamingPatternRule
        {
            TokenizedPattern = "DR_{Token}",
            AllowNumericTokenStart = true
        };

        var restrictiveResult = atom.Validate(CreateRequest(CreateTargetSet("DR_01"), restrictiveRule));
        var permissiveResult = atom.Validate(CreateRequest(CreateTargetSet("DR_01"), permissiveRule));

        Assert.False(restrictiveResult.Findings!.Single().Passed);
        Assert.True(permissiveResult.Findings!.Single().Passed);
    }

    [Fact]
    public void Validate_enforces_regular_expression()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_SingleDoor", "Door_Single"),
            new PatternContracts.NamingPatternRule
            {
                RegularExpression = @"^DR_[A-Za-z][A-Za-z0-9]*$"
            }));

        Assert.True(result.Findings!.Single(finding => finding.ObjectName == "DR_SingleDoor").Passed);
        Assert.False(result.Findings!.Single(finding => finding.ObjectName == "Door_Single").Passed);
    }

    [Fact]
    public void Validate_enforces_length_constraints()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_"),
            new PatternContracts.NamingPatternRule
            {
                MinimumLength = 5
            }));

        Assert.Equal(PatternContracts.NamingPatternValidationStatus.LengthViolation, result.Findings!.Single().Status);
        Assert.Equal(1, result.Statistics!.LengthViolations);
    }

    [Fact]
    public void Validate_enforces_allowed_characters()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_Door@01"),
            new PatternContracts.NamingPatternRule
            {
                AllowedCharacters = "A-Za-z0-9_"
            }));

        Assert.Equal(PatternContracts.NamingPatternValidationStatus.InvalidCharacter, result.Findings!.Single().Status);
    }

    [Fact]
    public void Validate_enforces_forbidden_characters()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR-Door"),
            new PatternContracts.NamingPatternRule
            {
                ForbiddenCharacters = ["-", " "]
            }));

        Assert.Equal(PatternContracts.NamingPatternValidationStatus.ForbiddenCharacter, result.Findings!.Single().Status);
    }

    [Fact]
    public void Validate_supports_tokenized_pattern()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_SingleDoor"),
            new PatternContracts.NamingPatternRule
            {
                TokenizedPattern = "DR_{Token}"
            }));

        Assert.True(result.Findings!.Single().Passed);
    }

    [Fact]
    public void Validate_generates_evidence_for_pattern_violations()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR-"),
            CreateDoorPatternRule()));

        Assert.Single(result.Evidence!);
        Assert.Equal(EvidenceSeverity.Error, result.Evidence![0].Severity);
        Assert.Equal("naming-engine", result.Evidence[0].Source!.EngineId);
    }

    [Fact]
    public void Validate_generates_statistics()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_SingleDoor", "DR-"),
            CreateDoorPatternRule()));

        Assert.Equal(2, result.Statistics!.ObjectsChecked);
        Assert.Equal(1, result.Statistics.ObjectsPassed);
        Assert.Equal(1, result.Statistics.ObjectsFailed);
    }

    [Fact]
    public void Validate_generates_diagnostics()
    {
        var atom = new NamingPatternValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_SingleDoor"),
            CreateDoorPatternRule()));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "NamingPatternValidation.Completed");
        Assert.Equal("target-set-naming-001", result.Metadata!["targetSetId"]);
    }

    [Fact]
    public void Validate_produces_deterministic_results()
    {
        var atom = new NamingPatternValidationAtom();
        var request = CreateRequest(
            CreateTargetSet("DR_SingleDoor", "DR-"),
            CreateDoorPatternRule());

        var first = atom.Validate(request);
        var second = atom.Validate(request);

        Assert.Equal(first.Findings![0].ObjectName, second.Findings![0].ObjectName);
        Assert.Equal(first.Evidence!.Count, second.Evidence!.Count);
        Assert.Equal(first.Statistics!.PatternViolations, second.Statistics!.PatternViolations);
    }

    [Fact]
    public void Validation_atom_does_not_contain_correction_or_renaming_methods()
    {
        var atomTypes = new[] { typeof(NamingPatternValidationAtom) };

        Assert.All(atomTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Rename", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Validation_atom_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(NamingPatternValidationAtom).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    private static PatternContracts.NamingPatternRule CreateDoorPatternRule()
    {
        return new PatternContracts.NamingPatternRule
        {
            TokenizedPattern = "DR_{Token}",
            RegularExpression = @"^DR_[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }

    private static NamingTargetSet CreateTargetSet(params string[] familyNames)
    {
        return new NamingTargetSet
        {
            TargetSetId = "target-set-naming-001",
            TargetFamilies = familyNames
                .Select((name, index) => new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier { Id = $"family-{index + 1:D3}", Kind = "family" },
                    Name = name
                })
                .ToArray()
        };
    }

    private static PatternContracts.NamingPatternValidationRequest CreateRequest(
        NamingTargetSet targetSet,
        PatternContracts.NamingPatternRule rule)
    {
        return new PatternContracts.NamingPatternValidationRequest
        {
            TargetSet = targetSet,
            Rule = rule,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-naming-pattern-001"
        };
    }
}
