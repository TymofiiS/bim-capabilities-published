using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Evidence;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;
using PatternContracts = BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Engines.Naming.Compliance;

namespace BIMCapabilities.Engines.Naming.Tests;

public class NamingComplianceEngineTests
{
    private static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 12, 0, 0, TimeSpan.Zero);

    private const string DoorPrefix = "DR_";
    private const string WindowPrefix = "WN_";

    [Fact]
    public void Evaluate_runs_prefix_only_workflow()
    {
        var engine = new NamingComplianceEngine();
        var result = engine.Evaluate(CreateRequest(
            CreateDoorTargetSet(),
            requiredPrefixes: [DoorPrefix],
            patternRule: null));

        Assert.NotNull(result.PrefixResult);
        Assert.Null(result.PatternResult);
        Assert.Equal(2, result.Findings!.Count);
        Assert.All(result.Findings, finding => Assert.Equal("prefix", finding.ValidationStage));
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "PrefixValidation.Completed");
    }

    [Fact]
    public void Evaluate_runs_pattern_only_workflow()
    {
        var engine = new NamingComplianceEngine();
        var result = engine.Evaluate(CreateRequest(
            CreateDoorTargetSet(),
            requiredPrefixes: null,
            patternRule: CreateDoorPatternRule()));

        Assert.Null(result.PrefixResult);
        Assert.NotNull(result.PatternResult);
        Assert.Equal(2, result.Findings!.Count);
        Assert.All(result.Findings, finding => Assert.Equal("pattern", finding.ValidationStage));
    }

    [Fact]
    public void Evaluate_runs_combined_workflow()
    {
        var engine = new NamingComplianceEngine();
        var result = engine.Evaluate(CreateDoorComplianceRequest(CreateDoorTargetSet()));

        Assert.NotNull(result.PrefixResult);
        Assert.NotNull(result.PatternResult);
        Assert.Equal(4, result.Findings!.Count);
        Assert.Equal(2, result.Statistics!.PrefixChecksRun);
        Assert.Equal(2, result.Statistics.PatternChecksRun);
    }

    [Fact]
    public void Evaluate_supports_door_family_mvp_scenario()
    {
        var engine = new NamingComplianceEngine();
        var result = engine.Evaluate(CreateDoorComplianceRequest(CreateDoorTargetSet()));

        Assert.Equal(NamingComplianceEngine.ComplianceEngineId, result.EngineId);
        Assert.Equal(100m, result.Summary!.CompliancePercentage);
        Assert.Equal(4, result.Summary.PassedChecks);
        Assert.Equal(0, result.Summary.FailedChecks);
        Assert.Equal(0, result.Summary.NamingViolations);
        Assert.Empty(result.Evidence!);
    }

    [Fact]
    public void Evaluate_supports_window_family_mvp_scenario()
    {
        var engine = new NamingComplianceEngine();
        var result = engine.Evaluate(CreateWindowComplianceRequest(CreateWindowTargetSet()));

        Assert.Equal(100m, result.Summary!.CompliancePercentage);
        Assert.Equal(0, result.Summary.NamingViolations);
        Assert.All(result.Findings!, finding => Assert.True(finding.Passed));
    }

    [Fact]
    public void Evaluate_aggregates_evidence_from_all_atoms()
    {
        var engine = new NamingComplianceEngine();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetFamilies =
            [
                CreateFamily("family-001", "Door_Single")
            ],
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "DR_SingleDoor")
            ]
        };

        var result = engine.Evaluate(CreateDoorComplianceRequest(targetSet));

        Assert.NotEmpty(result.Evidence!);
        Assert.Contains(result.Evidence!, record => record.EvidenceId.Contains("prefix-", StringComparison.Ordinal));
        Assert.Equal(EvidenceSeverity.Error, result.Evidence!.First().Severity);
    }

    [Fact]
    public void Evaluate_aggregates_diagnostics_from_all_atoms()
    {
        var engine = new NamingComplianceEngine();
        var result = engine.Evaluate(CreateDoorComplianceRequest(CreateDoorTargetSet()));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "PrefixValidation.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "NamingPatternValidation.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "NamingCompliance.Completed");
    }

    [Fact]
    public void Evaluate_aggregates_statistics_from_all_atoms()
    {
        var engine = new NamingComplianceEngine();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetFamilies =
            [
                CreateFamily("family-001", "Door_Single")
            ],
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "DR_SingleDoor")
            ]
        };

        var result = engine.Evaluate(CreateDoorComplianceRequest(targetSet));

        Assert.Equal(2, result.Statistics!.ObjectsChecked);
        Assert.Equal(1, result.Statistics.ObjectsFailed);
        Assert.Equal(4, result.Findings!.Count);
        Assert.Equal(1, result.Statistics.MissingPrefixCount);
    }

    [Fact]
    public void Evaluate_generates_compliance_summary()
    {
        var engine = new NamingComplianceEngine();
        var targetSet = CreateDoorTargetSet() with
        {
            TargetFamilies =
            [
                CreateFamily("family-001", "Door_Single")
            ],
            TargetTypes =
            [
                CreateFamilyType("family-type-001", "DR_SingleDoor")
            ]
        };

        var result = engine.Evaluate(CreateDoorComplianceRequest(targetSet));

        Assert.Equal(2, result.Summary!.ObjectsChecked);
        Assert.Equal(2, result.Summary.PassedChecks);
        Assert.Equal(2, result.Summary.FailedChecks);
        Assert.Equal(2, result.Summary.NamingViolations);
        Assert.Equal(50m, result.Summary.CompliancePercentage);
    }

    [Fact]
    public void Evaluate_produces_deterministic_results()
    {
        var engine = new NamingComplianceEngine();
        var request = CreateDoorComplianceRequest(CreateDoorTargetSet());

        var first = engine.Evaluate(request);
        var second = engine.Evaluate(request);

        Assert.Equal(first.Summary!.CompliancePercentage, second.Summary!.CompliancePercentage);
        Assert.Equal(first.Evidence!.Count, second.Evidence!.Count);
        Assert.Equal(first.Findings![0].ObjectName, second.Findings![0].ObjectName);
    }

    [Fact]
    public void Compliance_engine_does_not_contain_correction_or_renaming_methods()
    {
        var engineTypes = new[] { typeof(NamingComplianceEngine) };

        Assert.All(engineTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Rename", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Transaction", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Compliance_engine_does_not_reference_revit_assemblies()
    {
        var engineAssembly = typeof(NamingComplianceEngine).Assembly;
        var referencedAssemblies = engineAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    private static ComplianceContracts.NamingComplianceRequest CreateDoorComplianceRequest(NamingTargetSet targetSet)
    {
        return CreateRequest(
            targetSet,
            requiredPrefixes: [DoorPrefix],
            patternRule: CreateDoorPatternRule());
    }

    private static ComplianceContracts.NamingComplianceRequest CreateWindowComplianceRequest(NamingTargetSet targetSet)
    {
        return CreateRequest(
            targetSet,
            requiredPrefixes: [WindowPrefix],
            patternRule: CreateWindowPatternRule());
    }

    private static ComplianceContracts.NamingComplianceRequest CreateRequest(
        NamingTargetSet targetSet,
        IReadOnlyList<string>? requiredPrefixes,
        PatternContracts.NamingPatternRule? patternRule)
    {
        return new ComplianceContracts.NamingComplianceRequest
        {
            TargetSet = targetSet,
            RequiredPrefixes = requiredPrefixes,
            PatternRule = patternRule,
            ExecutedAt = ExecutedAt,
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-naming-compliance-001"
        };
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

    private static PatternContracts.NamingPatternRule CreateWindowPatternRule()
    {
        return new PatternContracts.NamingPatternRule
        {
            TokenizedPattern = "WN_{Token}",
            RegularExpression = @"^WN_[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }

    private static NamingTargetSet CreateDoorTargetSet()
    {
        return new NamingTargetSet
        {
            TargetSetId = "target-set-doors-001",
            TargetFamilies = [CreateFamily("family-001", "DR_SingleDoor")],
            TargetTypes = [CreateFamilyType("family-type-001", "DR_DoubleDoor")],
            SelectionMetadata = new Dictionary<string, string> { ["scope"] = "all-door-families" }
        };
    }

    private static NamingTargetSet CreateWindowTargetSet()
    {
        return new NamingTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetFamilies = [CreateFamily("family-002", "WN_Window01")],
            TargetTypes = [CreateFamilyType("family-type-003", "WN_Window02")],
            SelectionMetadata = new Dictionary<string, string> { ["scope"] = "all-window-families" }
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
}
