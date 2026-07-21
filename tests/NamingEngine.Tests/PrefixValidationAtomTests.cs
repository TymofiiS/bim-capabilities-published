using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Evidence;
using PrefixContracts = BIMCapabilities.Contracts.Engines.Naming.Prefix;
using BIMCapabilities.Engines.Naming.Atoms.Prefix;

namespace BIMCapabilities.Engines.Naming.Tests;

public class PrefixValidationAtomTests
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_passes_when_name_starts_with_valid_dr_prefix()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_SingleDoor"),
            ["DR_"]));

        Assert.Equal(PrefixValidationAtom.ValidationAtomId, result.AtomId);
        Assert.Single(result.Findings!);
        Assert.True(result.Findings![0].Passed);
        Assert.Equal(PrefixContracts.PrefixValidationStatus.Valid, result.Findings[0].Status);
        Assert.Equal("DR_", result.Findings[0].MatchedPrefix);
        Assert.Empty(result.Evidence!);
        Assert.Equal(1, result.Statistics!.ObjectsPassed);
    }

    [Fact]
    public void Validate_passes_for_dr_double_door_mvp_example()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_DoubleDoor"),
            ["DR_"]));

        Assert.True(result.Findings!.Single().Passed);
    }

    [Fact]
    public void Validate_detects_invalid_dr_prefix()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("Door_Single"),
            ["DR_"]));

        Assert.Single(result.Findings!);
        Assert.False(result.Findings![0].Passed);
        Assert.Equal(PrefixContracts.PrefixValidationStatus.MissingPrefix, result.Findings[0].Status);
        Assert.Single(result.Evidence!);
        Assert.Equal(EvidenceSeverity.Error, result.Evidence![0].Severity);
        Assert.Equal(1, result.Statistics!.MissingPrefixCount);
    }

    [Fact]
    public void Validate_passes_when_name_starts_with_valid_wn_prefix()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("WN_Window01"),
            ["WN_"]));

        Assert.True(result.Findings!.Single().Passed);
        Assert.Equal("WN_", result.Findings!.Single().MatchedPrefix);
    }

    [Fact]
    public void Validate_detects_invalid_wn_prefix()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("Window_01"),
            ["WN_"]));

        Assert.False(result.Findings!.Single().Passed);
        Assert.Equal(PrefixContracts.PrefixValidationStatus.MissingPrefix, result.Findings!.Single().Status);
    }

    [Fact]
    public void Validate_detects_empty_name()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("   "),
            ["DR_"]));

        Assert.Equal(PrefixContracts.PrefixValidationStatus.EmptyName, result.Findings!.Single().Status);
        Assert.Equal(1, result.Statistics!.MissingPrefixCount);
    }

    [Fact]
    public void Validate_supports_multiple_allowed_prefixes()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("WN_Window01", "DR_SingleDoor"),
            ["DR_", "WN_"]));

        Assert.Equal(2, result.Findings!.Count);
        Assert.All(result.Findings, finding => Assert.True(finding.Passed));
    }

    [Fact]
    public void Validate_supports_case_insensitive_mode()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("dr_singledoor"),
            ["DR_"],
            caseSensitive: false));

        Assert.True(result.Findings!.Single().Passed);
    }

    [Fact]
    public void Validate_supports_case_sensitive_mode()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("dr_singledoor"),
            ["DR_"],
            caseSensitive: true));

        Assert.False(result.Findings!.Single().Passed);
        Assert.Equal(PrefixContracts.PrefixValidationStatus.InvalidPrefix, result.Findings!.Single().Status);
        Assert.Equal(1, result.Statistics!.InvalidPrefixCount);
    }

    [Fact]
    public void Validate_checks_family_and_family_type_names()
    {
        var atom = new PrefixValidationAtom();
        var targetSet = new NamingTargetSet
        {
            TargetSetId = "target-set-doors-001",
            TargetFamilies =
            [
                CreateFamily("family-001", "DR_SingleDoor")
            ],
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "Door_Single")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["DR_"]));

        Assert.Equal(2, result.Findings!.Count);
        Assert.True(result.Findings!.Single(finding => finding.ObjectKind == "family").Passed);
        Assert.False(result.Findings.Single(finding => finding.ObjectKind == "familyType").Passed);
    }

    [Fact]
    public void Validate_generates_evidence_for_failed_names()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("Door_Single"),
            ["DR_"]));

        Assert.Single(result.Evidence!);
        Assert.Contains("Door_Single", result.Evidence![0].Message, StringComparison.Ordinal);
        Assert.Equal("naming-engine", result.Evidence[0].Source!.EngineId);
    }

    [Fact]
    public void Validate_generates_statistics()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_SingleDoor", "Door_Single"),
            ["DR_"]));

        Assert.Equal(2, result.Statistics!.ObjectsChecked);
        Assert.Equal(1, result.Statistics.ObjectsPassed);
        Assert.Equal(1, result.Statistics.ObjectsFailed);
        Assert.Equal(1, result.Statistics.MissingPrefixCount);
    }

    [Fact]
    public void Validate_generates_diagnostics()
    {
        var atom = new PrefixValidationAtom();
        var result = atom.Validate(CreateRequest(
            CreateTargetSet("DR_SingleDoor"),
            ["DR_"]));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "PrefixValidation.Completed");
        Assert.Equal("target-set-naming-001", result.Metadata!["targetSetId"]);
    }

    [Fact]
    public void Validate_produces_deterministic_results()
    {
        var atom = new PrefixValidationAtom();
        var request = CreateRequest(
            CreateTargetSet("DR_SingleDoor", "Door_Single"),
            ["DR_"]);

        var first = atom.Validate(request);
        var second = atom.Validate(request);

        Assert.Equal(first.Findings![0].ObjectName, second.Findings![0].ObjectName);
        Assert.Equal(first.Evidence!.Count, second.Evidence!.Count);
        Assert.Equal(first.Statistics!.MissingPrefixCount, second.Statistics!.MissingPrefixCount);
    }

    [Fact]
    public void Validation_atom_does_not_contain_correction_or_renaming_methods()
    {
        var atomTypes = new[] { typeof(PrefixValidationAtom) };

        Assert.All(atomTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Rename", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Pattern", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Validation_atom_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(PrefixValidationAtom).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_includes_category_name_in_evidence_when_available()
    {
        var atom = new PrefixValidationAtom();
        var targetSet = new NamingTargetSet
        {
            TargetSetId = "target-set-windows-001",
            SelectionMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["category"] = "Windows"
            },
            TargetFamilies =
            [
                CreateFamily("family-001", "Window_01")
            ]
        };

        var result = atom.Validate(CreateRequest(targetSet, ["WD_"]));

        Assert.Single(result.Evidence!);
        Assert.Equal("Windows", result.Evidence![0].StructuredData!["categoryName"]);
        Assert.Equal("Windows", result.Evidence[0].Target!.TargetSetDescription);
    }

    [Fact]
    public void Validate_respects_prefix_fix_scope_for_types_only()
    {
        var atom = new PrefixValidationAtom();
        var targetSet = new NamingTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetFamilies =
            [
                CreateFamily("family-001", "M_Window-Fixed")
            ],
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "850 x 900mm")
            ]
        };

        var result = atom.Validate(new PrefixContracts.PrefixValidationRequest
        {
            TargetSet = targetSet,
            RequiredPrefixes = ["WD_"],
            PrefixFixScope = PrefixFixScope.Type,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-prefix-validation-scope-type"
        });

        Assert.Single(result.Findings!);
        Assert.Equal("familyType", result.Findings![0].ObjectKind);
        Assert.Equal("850 x 900mm", result.Findings[0].ObjectName);
        Assert.False(result.Findings[0].Passed);
    }

    [Fact]
    public void Validate_respects_prefix_fix_scope_for_families_only()
    {
        var atom = new PrefixValidationAtom();
        var targetSet = new NamingTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetFamilies =
            [
                CreateFamily("family-001", "M_Window-Fixed")
            ],
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "850 x 900mm")
            ]
        };

        var result = atom.Validate(new PrefixContracts.PrefixValidationRequest
        {
            TargetSet = targetSet,
            RequiredPrefixes = ["WD_"],
            PrefixFixScope = PrefixFixScope.Family,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-prefix-validation-scope-family"
        });

        Assert.Single(result.Findings!);
        Assert.Equal("family", result.Findings![0].ObjectKind);
        Assert.Equal("M_Window-Fixed", result.Findings[0].ObjectName);
        Assert.False(result.Findings[0].Passed);
    }

    private static NamingTargetSet CreateTargetSet(params string[] familyNames)
    {
        return new NamingTargetSet
        {
            TargetSetId = "target-set-naming-001",
            TargetFamilies = familyNames
                .Select((name, index) => CreateFamily($"family-{index + 1:D3}", name))
                .ToArray()
        };
    }

    private static NormalizedFamily CreateFamily(string id, string name)
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier { Id = id, Kind = "family" },
            Name = name
        };
    }

    private static NormalizedFamilyType CreateFamilyType(string id, string name)
    {
        return new NormalizedFamilyType
        {
            Identity = new NormalizedIdentifier { Id = id, Kind = "familyType" },
            Name = name
        };
    }

    private static PrefixContracts.PrefixValidationRequest CreateRequest(
        NamingTargetSet targetSet,
        IReadOnlyList<string> requiredPrefixes,
        bool caseSensitive = false)
    {
        return new PrefixContracts.PrefixValidationRequest
        {
            TargetSet = targetSet,
            RequiredPrefixes = requiredPrefixes,
            CaseSensitive = caseSensitive,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-prefix-validation-001"
        };
    }
}
