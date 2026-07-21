using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Contracts.Engines.Naming.Prefix;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;

namespace BIMCapabilities.Engines.Naming.Compliance;

internal static class NamingComplianceEngineSupport
{
    internal static ComplianceContracts.NamingComplianceResult CreateResult(
        string engineId,
        ComplianceContracts.NamingComplianceRequest request,
        PrefixValidationResult? prefixResult,
        NamingPatternValidationResult? patternResult)
    {
        var findings = BuildFindings(prefixResult, patternResult);
        var evidence = AggregateEvidence(prefixResult, patternResult);
        var diagnostics = AggregateDiagnostics(engineId, prefixResult, patternResult);
        var statistics = BuildStatistics(prefixResult, patternResult, findings);
        var summary = BuildSummary(findings, statistics);

        return new ComplianceContracts.NamingComplianceResult
        {
            EngineId = engineId,
            PrefixResult = prefixResult,
            PatternResult = patternResult,
            Findings = findings,
            Evidence = evidence,
            Diagnostics = diagnostics,
            Statistics = statistics,
            Summary = summary,
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<ComplianceContracts.NamingComplianceFinding> BuildFindings(
        PrefixValidationResult? prefixResult,
        NamingPatternValidationResult? patternResult)
    {
        var findings = new List<ComplianceContracts.NamingComplianceFinding>();

        if (prefixResult?.Findings is not null)
        {
            foreach (var finding in prefixResult.Findings)
            {
                findings.Add(new ComplianceContracts.NamingComplianceFinding
                {
                    ValidationStage = "prefix",
                    ObjectId = finding.ObjectId,
                    ObjectKind = finding.ObjectKind,
                    ObjectName = finding.ObjectName,
                    Passed = finding.Passed,
                    Status = finding.Status.ToString(),
                    Message = finding.Passed
                        ? $"Object name '{finding.ObjectName ?? finding.ObjectId}' satisfies prefix requirements."
                        : BuildPrefixFailureMessage(finding)
                });
            }
        }

        if (patternResult?.Findings is not null)
        {
            foreach (var finding in patternResult.Findings)
            {
                findings.Add(new ComplianceContracts.NamingComplianceFinding
                {
                    ValidationStage = "pattern",
                    ObjectId = finding.ObjectId,
                    ObjectKind = finding.ObjectKind,
                    ObjectName = finding.ObjectName,
                    Passed = finding.Passed,
                    Status = finding.Status.ToString(),
                    Message = finding.Passed
                        ? $"Object name '{finding.ObjectName ?? finding.ObjectId}' satisfies naming pattern requirements."
                        : finding.ViolationReason
                });
            }
        }

        return findings
            .OrderBy(finding => finding.ValidationStage, StringComparer.Ordinal)
            .ThenBy(finding => finding.ObjectKind, StringComparer.Ordinal)
            .ThenBy(finding => finding.ObjectId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<EvidenceRecord> AggregateEvidence(
        PrefixValidationResult? prefixResult,
        NamingPatternValidationResult? patternResult)
    {
        var evidence = new List<EvidenceRecord>();

        if (prefixResult?.Evidence is not null)
        {
            evidence.AddRange(prefixResult.Evidence);
        }

        if (patternResult?.Evidence is not null)
        {
            evidence.AddRange(patternResult.Evidence);
        }

        return evidence
            .OrderBy(record => record.EvidenceId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<NamingEngineDiagnostic> AggregateDiagnostics(
        string engineId,
        PrefixValidationResult? prefixResult,
        NamingPatternValidationResult? patternResult)
    {
        var diagnostics = new List<NamingEngineDiagnostic>();

        if (prefixResult?.Diagnostics is not null)
        {
            diagnostics.AddRange(prefixResult.Diagnostics);
        }

        if (patternResult?.Diagnostics is not null)
        {
            diagnostics.AddRange(patternResult.Diagnostics);
        }

        diagnostics.Add(new NamingEngineDiagnostic
        {
            Code = "NamingCompliance.Completed",
            Message = BuildCompletionMessage(engineId, prefixResult, patternResult),
            Severity = NamingEngineDiagnosticSeverity.Information,
            Location = $"engine:{engineId}"
        });

        return diagnostics;
    }

    internal static ComplianceContracts.NamingComplianceStatistics BuildStatistics(
        PrefixValidationResult? prefixResult,
        NamingPatternValidationResult? patternResult,
        IReadOnlyList<ComplianceContracts.NamingComplianceFinding> findings)
    {
        var objectIds = findings
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var objectsFailed = findings
            .Where(finding => !finding.Passed)
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return new ComplianceContracts.NamingComplianceStatistics
        {
            ObjectsChecked = objectIds.Length,
            ObjectsPassed = objectIds.Length - objectsFailed,
            ObjectsFailed = objectsFailed,
            PrefixChecksRun = prefixResult?.Findings?.Count ?? 0,
            PatternChecksRun = patternResult?.Findings?.Count ?? 0,
            MissingPrefixCount = prefixResult?.Statistics?.MissingPrefixCount ?? 0,
            InvalidPrefixCount = prefixResult?.Statistics?.InvalidPrefixCount ?? 0,
            PatternViolations = patternResult?.Statistics?.PatternViolations ?? 0,
            InvalidCharacterViolations = patternResult?.Statistics?.InvalidCharacterViolations ?? 0,
            LengthViolations = patternResult?.Statistics?.LengthViolations ?? 0
        };
    }

    internal static ComplianceContracts.NamingComplianceSummary BuildSummary(
        IReadOnlyList<ComplianceContracts.NamingComplianceFinding> findings,
        ComplianceContracts.NamingComplianceStatistics statistics)
    {
        var passedChecks = findings.Count(finding => finding.Passed);
        var failedChecks = findings.Count - passedChecks;
        var compliancePercentage = findings.Count == 0
            ? 100m
            : Math.Round((decimal)passedChecks / findings.Count * 100m, 2, MidpointRounding.AwayFromZero);

        return new ComplianceContracts.NamingComplianceSummary
        {
            ObjectsChecked = statistics.ObjectsChecked,
            PassedChecks = passedChecks,
            FailedChecks = failedChecks,
            CompliancePercentage = compliancePercentage,
            NamingViolations = failedChecks
        };
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        ComplianceContracts.NamingComplianceRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["complianceOperation"] = "naming-compliance",
            ["targetSetId"] = request.TargetSet.TargetSetId,
            ["caseSensitive"] = request.CaseSensitive.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.RuleId))
        {
            metadata["ruleId"] = request.RuleId;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata["correlationId"] = request.CorrelationId;
        }

        if (request.Metadata is not null)
        {
            foreach (var pair in request.Metadata.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return metadata;
    }

    private static string BuildCompletionMessage(
        string engineId,
        PrefixValidationResult? prefixResult,
        NamingPatternValidationResult? patternResult)
    {
        var stages = new List<string>();

        if (prefixResult is not null)
        {
            stages.Add("prefix");
        }

        if (patternResult is not null)
        {
            stages.Add("pattern");
        }

        var stageList = stages.Count == 0 ? "none" : string.Join(", ", stages);
        return $"Naming compliance engine '{engineId}' completed stages: {stageList}.";
    }

    private static string BuildPrefixFailureMessage(PrefixValidationFinding finding)
    {
        return finding.Status switch
        {
            PrefixValidationStatus.EmptyName =>
                $"Object '{finding.ObjectId}' has an empty name.",
            PrefixValidationStatus.InvalidPrefix =>
                $"Object name '{finding.ObjectName ?? finding.ObjectId}' uses an incorrect prefix.",
            PrefixValidationStatus.MissingPrefix =>
                $"Object name '{finding.ObjectName ?? finding.ObjectId}' is missing a required prefix.",
            _ => $"Prefix validation failed for '{finding.ObjectName ?? finding.ObjectId}'."
        };
    }
}
