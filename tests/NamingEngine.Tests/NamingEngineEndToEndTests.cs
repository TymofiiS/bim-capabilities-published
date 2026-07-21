using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Contracts.Engines.Naming.Prefix;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Engines.Naming.Atoms.Pattern;
using BIMCapabilities.Engines.Naming.Atoms.Prefix;
using BIMCapabilities.Engines.Naming.Compliance;
using BIMCapabilities.Engines.Naming.Tests.Builders;
using BIMCapabilities.Engines.Naming.Tests.Fixtures;

namespace BIMCapabilities.Engines.Naming.Tests;

public class NamingEngineEndToEndTests
{
    private readonly NamingComplianceEngine _complianceEngine = new();
    private readonly PrefixValidationAtom _prefixAtom = new();
    private readonly NamingPatternValidationAtom _patternAtom = new();

    [Fact]
    public void Valid_door_families_end_to_end_workflow_passes()
    {
        var request = ValidDoorFamiliesFixture.CreateComplianceRequest();
        var result = RunEndToEndWorkflow(request);

        Assert.Equal(NamingComplianceEngine.ComplianceEngineId, result.ComplianceResult.EngineId);
        Assert.Equal(100m, result.ComplianceResult.Summary!.CompliancePercentage);
        Assert.Equal(0, result.ComplianceResult.Summary.NamingViolations);
        Assert.Equal(4, result.ComplianceResult.Summary.ObjectsChecked);
        Assert.Equal(8, result.ComplianceResult.Summary.PassedChecks);
        Assert.Equal(0, result.ComplianceResult.Summary.FailedChecks);
        Assert.Empty(result.ComplianceResult.Evidence!);
        Assert.All(result.ComplianceResult.Findings!, finding => Assert.True(finding.Passed));
        Assert.All(result.PrefixResult!.Findings!, finding => Assert.True(finding.Passed));
        Assert.All(result.PatternResult!.Findings!, finding => Assert.True(finding.Passed));
    }

    [Fact]
    public void Invalid_door_families_end_to_end_workflow_fails()
    {
        var request = InvalidDoorFamiliesFixture.CreateComplianceRequest();
        var result = RunEndToEndWorkflow(request);

        Assert.Equal(0m, result.ComplianceResult.Summary!.CompliancePercentage);
        Assert.Equal(4, result.ComplianceResult.Summary.ObjectsChecked);
        Assert.Equal(8, result.ComplianceResult.Summary.FailedChecks);
        Assert.Equal(8, result.ComplianceResult.Summary.NamingViolations);
        Assert.NotEmpty(result.ComplianceResult.Evidence!);
        Assert.Contains(result.ComplianceResult.Evidence!, record => record.EvidenceId.Contains("prefix-", StringComparison.Ordinal));
        Assert.Equal(4, result.ComplianceResult.Statistics!.MissingPrefixCount);
    }

    [Fact]
    public void Valid_window_families_end_to_end_workflow_passes()
    {
        var request = ValidWindowFamiliesFixture.CreateComplianceRequest();
        var result = RunEndToEndWorkflow(request);

        Assert.Equal(100m, result.ComplianceResult.Summary!.CompliancePercentage);
        Assert.Equal(0, result.ComplianceResult.Summary.NamingViolations);
        Assert.Equal(4, result.ComplianceResult.Summary.ObjectsChecked);
        Assert.Equal(8, result.ComplianceResult.Summary.PassedChecks);
        Assert.Empty(result.ComplianceResult.Evidence!);
        Assert.All(result.ComplianceResult.Findings!, finding => Assert.True(finding.Passed));
    }

    [Fact]
    public void Invalid_window_families_end_to_end_workflow_fails()
    {
        var request = InvalidWindowFamiliesFixture.CreateComplianceRequest();
        var result = RunEndToEndWorkflow(request);

        Assert.Equal(0m, result.ComplianceResult.Summary!.CompliancePercentage);
        Assert.Equal(3, result.ComplianceResult.Summary.ObjectsChecked);
        Assert.Equal(6, result.ComplianceResult.Summary.FailedChecks);
        Assert.Equal(6, result.ComplianceResult.Summary.NamingViolations);
        Assert.NotEmpty(result.ComplianceResult.Evidence!);
        Assert.Equal(3, result.ComplianceResult.Statistics!.MissingPrefixCount);
    }

    [Fact]
    public void Mixed_naming_end_to_end_workflow_reports_partial_compliance()
    {
        var request = MixedNamingFixture.CreateComplianceRequest();
        var result = RunEndToEndWorkflow(request);

        Assert.Equal(50m, result.ComplianceResult.Summary!.CompliancePercentage);
        Assert.Equal(4, result.ComplianceResult.Summary.ObjectsChecked);
        Assert.Equal(4, result.ComplianceResult.Summary.PassedChecks);
        Assert.Equal(4, result.ComplianceResult.Summary.FailedChecks);
        Assert.Equal(4, result.ComplianceResult.Summary.NamingViolations);
        Assert.Equal(2, result.ComplianceResult.Statistics!.ObjectsPassed);
        Assert.Equal(2, result.ComplianceResult.Statistics.ObjectsFailed);
    }

    [Fact]
    public void Large_naming_dataset_end_to_end_workflow_scales_deterministically()
    {
        var request = LargeNamingFixture.CreateComplianceRequest();
        var result = RunEndToEndWorkflow(request);

        Assert.Equal(LargeNamingFixture.ValidFamilyCount + LargeNamingFixture.InvalidFamilyCount, result.ComplianceResult.Summary!.ObjectsChecked);
        Assert.Equal(50m, result.ComplianceResult.Summary.CompliancePercentage);
        Assert.Equal(LargeNamingFixture.ValidFamilyCount * 2, result.ComplianceResult.Summary.PassedChecks);
        Assert.Equal(LargeNamingFixture.InvalidFamilyCount * 2, result.ComplianceResult.Summary.FailedChecks);
        Assert.Equal(LargeNamingFixture.InvalidFamilyCount, result.ComplianceResult.Statistics!.ObjectsFailed);
        Assert.Equal(LargeNamingFixture.ValidFamilyCount, result.ComplianceResult.Statistics.ObjectsPassed);
    }

    [Fact]
    public void End_to_end_workflow_aggregates_evidence_from_prefix_and_pattern_atoms()
    {
        var request = InvalidDoorFamiliesFixture.CreateComplianceRequest();
        var result = _complianceEngine.Evaluate(request);

        Assert.NotEmpty(result.Evidence!);
        Assert.Contains(result.Evidence!, record => record.EvidenceId.Contains("prefix-", StringComparison.Ordinal));
        Assert.Contains(result.Evidence!, record => record.EvidenceId.Contains("pattern-", StringComparison.Ordinal));
        Assert.All(result.Evidence!, record => Assert.Equal(EvidenceSeverity.Error, record.Severity));
    }

    [Fact]
    public void End_to_end_workflow_aggregates_diagnostics_from_all_stages()
    {
        var request = ValidDoorFamiliesFixture.CreateComplianceRequest();
        var result = _complianceEngine.Evaluate(request);

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "PrefixValidation.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "NamingPatternValidation.Completed");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "NamingCompliance.Completed");
    }

    [Fact]
    public void End_to_end_workflow_aggregates_statistics_from_all_stages()
    {
        var request = MixedNamingFixture.CreateComplianceRequest();
        var result = _complianceEngine.Evaluate(request);

        Assert.Equal(4, result.Statistics!.ObjectsChecked);
        Assert.Equal(4, result.Statistics.PrefixChecksRun);
        Assert.Equal(4, result.Statistics.PatternChecksRun);
        Assert.Equal(2, result.Statistics.MissingPrefixCount);
        Assert.True(result.Statistics.PatternViolations > 0);
    }

    [Fact]
    public void End_to_end_workflow_generates_compliance_summary()
    {
        var request = MixedNamingFixture.CreateComplianceRequest();
        var result = _complianceEngine.Evaluate(request);

        Assert.NotNull(result.Summary);
        Assert.Equal(result.Statistics!.ObjectsChecked, result.Summary.ObjectsChecked);
        Assert.Equal(result.Findings!.Count(finding => finding.Passed), result.Summary.PassedChecks);
        Assert.Equal(result.Findings!.Count(finding => !finding.Passed), result.Summary.FailedChecks);
        Assert.Equal(result.Summary.FailedChecks, result.Summary.NamingViolations);
    }

    [Fact]
    public void End_to_end_workflow_produces_deterministic_results()
    {
        var request = LargeNamingFixture.CreateComplianceRequest();

        var first = _complianceEngine.Evaluate(request);
        var second = _complianceEngine.Evaluate(request);

        Assert.Equal(
            SerializeDeterministicSnapshot(first),
            SerializeDeterministicSnapshot(second));
    }

    [Fact]
    public void Door_rule_dr_prefix_is_enforced_end_to_end()
    {
        var request = ValidDoorFamiliesFixture.CreateComplianceRequest();
        var result = _complianceEngine.Evaluate(request);

        Assert.All(
            result.PrefixResult!.Findings!,
            finding => Assert.Equal("DR_", finding.MatchedPrefix));
    }

    [Fact]
    public void Window_rule_wn_prefix_is_enforced_end_to_end()
    {
        var request = ValidWindowFamiliesFixture.CreateComplianceRequest();
        var result = _complianceEngine.Evaluate(request);

        Assert.All(
            result.PrefixResult!.Findings!,
            finding => Assert.Equal("WN_", finding.MatchedPrefix));
    }

    private EndToEndWorkflowResult RunEndToEndWorkflow(NamingComplianceRequest request)
    {
        var prefixResult = request.RequiredPrefixes is { Count: > 0 }
            ? _prefixAtom.Validate(new PrefixValidationRequest
            {
                TargetSet = request.TargetSet,
                RequiredPrefixes = request.RequiredPrefixes,
                CaseSensitive = request.CaseSensitive,
                ExecutedAt = request.ExecutedAt,
                RuleId = request.RuleId,
                CorrelationId = request.CorrelationId,
                Metadata = request.Metadata
            })
            : null;

        var patternResult = request.PatternRule is not null
            ? _patternAtom.Validate(new NamingPatternValidationRequest
            {
                TargetSet = request.TargetSet,
                Rule = request.PatternRule,
                ExecutedAt = request.ExecutedAt,
                RuleId = request.RuleId,
                CorrelationId = request.CorrelationId,
                Metadata = request.Metadata
            })
            : null;

        var complianceResult = _complianceEngine.Evaluate(request);

        return new EndToEndWorkflowResult(prefixResult, patternResult, complianceResult);
    }

    private static string SerializeDeterministicSnapshot(NamingComplianceResult result)
    {
        var snapshot = new
        {
            result.EngineId,
            Summary = result.Summary is null
                ? null
                : new
                {
                    result.Summary.ObjectsChecked,
                    result.Summary.PassedChecks,
                    result.Summary.FailedChecks,
                    result.Summary.CompliancePercentage,
                    result.Summary.NamingViolations
                },
            Findings = result.Findings?
                .Select(finding => new
                {
                    finding.ValidationStage,
                    finding.ObjectId,
                    finding.ObjectKind,
                    finding.ObjectName,
                    finding.Passed,
                    finding.Status
                })
                .ToArray(),
            Evidence = result.Evidence?
                .Select(record => new
                {
                    record.EvidenceId,
                    record.Severity,
                    record.Message
                })
                .ToArray(),
            Diagnostics = result.Diagnostics?
                .Select(diagnostic => new
                {
                    diagnostic.Code,
                    diagnostic.Message,
                    diagnostic.Severity
                })
                .ToArray(),
            Statistics = result.Statistics is null
                ? null
                : new
                {
                    result.Statistics.ObjectsChecked,
                    result.Statistics.ObjectsPassed,
                    result.Statistics.ObjectsFailed,
                    result.Statistics.PrefixChecksRun,
                    result.Statistics.PatternChecksRun,
                    result.Statistics.MissingPrefixCount,
                    result.Statistics.PatternViolations
                }
        };

        return JsonSerializer.Serialize(snapshot);
    }

    private sealed record EndToEndWorkflowResult(
        PrefixValidationResult? PrefixResult,
        NamingPatternValidationResult? PatternResult,
        NamingComplianceResult ComplianceResult);
}
