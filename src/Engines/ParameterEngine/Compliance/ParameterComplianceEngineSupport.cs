using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Engines.Parameter.Existence;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using BIMCapabilities.Contracts.Engines.Parameter.Value;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;

namespace BIMCapabilities.Engines.Parameter.Compliance;

internal static class ParameterComplianceEngineSupport
{
    internal static ComplianceContracts.ParameterComplianceResult CreateResult(
        string engineId,
        ComplianceContracts.ParameterComplianceRequest request,
        ParameterExistenceResult? existenceResult,
        SharedParameterValidationResult? sharedParameterResult,
        ParameterValueValidationResult? valueResult)
    {
        var findings = BuildFindings(existenceResult, sharedParameterResult, valueResult);
        var evidence = AggregateEvidence(existenceResult, sharedParameterResult, valueResult);
        var diagnostics = AggregateDiagnostics(engineId, existenceResult, sharedParameterResult, valueResult);
        var statistics = BuildStatistics(existenceResult, sharedParameterResult, valueResult, findings);
        var summary = BuildSummary(findings, statistics);

        return new ComplianceContracts.ParameterComplianceResult
        {
            EngineId = engineId,
            ExistenceResult = existenceResult,
            SharedParameterResult = sharedParameterResult,
            ValueResult = valueResult,
            Findings = findings,
            Evidence = evidence,
            Diagnostics = diagnostics,
            Statistics = statistics,
            Summary = summary,
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<ComplianceContracts.ParameterComplianceFinding> BuildFindings(
        ParameterExistenceResult? existenceResult,
        SharedParameterValidationResult? sharedParameterResult,
        ParameterValueValidationResult? valueResult)
    {
        var findings = new List<ComplianceContracts.ParameterComplianceFinding>();

        if (existenceResult?.Findings is not null)
        {
            foreach (var finding in existenceResult.Findings)
            {
                findings.Add(new ComplianceContracts.ParameterComplianceFinding
                {
                    ValidationStage = "existence",
                    ObjectId = finding.ObjectId,
                    ObjectKind = finding.ObjectKind,
                    ObjectName = finding.ObjectName,
                    ParameterName = finding.ParameterName,
                    Passed = finding.Exists,
                    Status = finding.Exists ? "Present" : "Missing",
                    Message = finding.Exists
                        ? $"Parameter '{finding.ParameterName}' exists."
                        : $"Required parameter '{finding.ParameterName}' is missing."
                });
            }
        }

        if (sharedParameterResult?.Findings is not null)
        {
            foreach (var finding in sharedParameterResult.Findings)
            {
                findings.Add(new ComplianceContracts.ParameterComplianceFinding
                {
                    ValidationStage = "shared-parameter",
                    ObjectId = finding.ObjectId,
                    ObjectKind = finding.ObjectKind,
                    ObjectName = finding.ObjectName,
                    ParameterName = finding.ParameterName,
                    Passed = finding.Passed,
                    Status = finding.Status.ToString(),
                    Message = finding.Passed
                        ? $"Shared parameter '{finding.ParameterName}' is valid."
                        : $"Shared parameter '{finding.ParameterName}' failed validation."
                });
            }
        }

        if (valueResult?.Findings is not null)
        {
            foreach (var finding in valueResult.Findings)
            {
                findings.Add(new ComplianceContracts.ParameterComplianceFinding
                {
                    ValidationStage = "value",
                    ObjectId = finding.ObjectId,
                    ObjectKind = finding.ObjectKind,
                    ObjectName = finding.ObjectName,
                    ParameterName = finding.ParameterName,
                    Passed = finding.Passed,
                    Status = finding.Status.ToString(),
                    Message = finding.Passed
                        ? $"Parameter '{finding.ParameterName}' has a valid value."
                        : finding.ViolationReason
                });
            }
        }

        return findings
            .OrderBy(finding => finding.ValidationStage, StringComparer.Ordinal)
            .ThenBy(finding => finding.ObjectId, StringComparer.Ordinal)
            .ThenBy(finding => finding.ParameterName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static IReadOnlyList<EvidenceRecord> AggregateEvidence(
        ParameterExistenceResult? existenceResult,
        SharedParameterValidationResult? sharedParameterResult,
        ParameterValueValidationResult? valueResult)
    {
        var evidence = new List<EvidenceRecord>();

        if (existenceResult?.Evidence is not null)
        {
            evidence.AddRange(existenceResult.Evidence);
        }

        if (sharedParameterResult?.Evidence is not null)
        {
            evidence.AddRange(sharedParameterResult.Evidence);
        }

        if (valueResult?.Evidence is not null)
        {
            evidence.AddRange(valueResult.Evidence);
        }

        return DeduplicateParameterEvidence(evidence);
    }

    internal static IReadOnlyList<EvidenceRecord> DeduplicateParameterEvidence(IReadOnlyList<EvidenceRecord> evidence)
    {
        var selected = new Dictionary<string, EvidenceRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in evidence.OrderBy(record => GetParameterEvidencePriority(record)))
        {
            var key = BuildParameterEvidenceKey(record);
            if (!selected.ContainsKey(key))
            {
                selected.Add(key, record);
            }
        }

        return selected.Values
            .OrderBy(record => record.EvidenceId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildParameterEvidenceKey(EvidenceRecord record)
    {
        var targetId = record.Target?.TargetId ?? record.Target?.TargetName ?? "unknown";
        var parameterName = record.StructuredData is not null
            && record.StructuredData.TryGetValue("parameterName", out var name)
            ? name
            : record.EvidenceId;

        return $"{targetId}|{parameterName}".ToLowerInvariant();
    }

    private static int GetParameterEvidencePriority(EvidenceRecord record)
    {
        var evidenceId = record.EvidenceId.ToLowerInvariant();
        if (evidenceId.Contains("parameter-missing", StringComparison.Ordinal))
        {
            return 0;
        }

        if (evidenceId.Contains("shared-parameter", StringComparison.Ordinal))
        {
            return 1;
        }

        if (evidenceId.Contains("parameter-value", StringComparison.Ordinal))
        {
            return 2;
        }

        return 3;
    }

    internal static IReadOnlyList<ParameterEngineDiagnostic> AggregateDiagnostics(
        string engineId,
        ParameterExistenceResult? existenceResult,
        SharedParameterValidationResult? sharedParameterResult,
        ParameterValueValidationResult? valueResult)
    {
        var diagnostics = new List<ParameterEngineDiagnostic>();

        if (existenceResult?.Diagnostics is not null)
        {
            diagnostics.AddRange(existenceResult.Diagnostics);
        }

        if (sharedParameterResult?.Diagnostics is not null)
        {
            diagnostics.AddRange(sharedParameterResult.Diagnostics);
        }

        if (valueResult?.Diagnostics is not null)
        {
            diagnostics.AddRange(valueResult.Diagnostics);
        }

        diagnostics.Add(new ParameterEngineDiagnostic
        {
            Code = "ParameterCompliance.Completed",
            Message = BuildCompletionMessage(engineId, existenceResult, sharedParameterResult, valueResult),
            Severity = ParameterEngineDiagnosticSeverity.Information,
            Location = $"engine:{engineId}"
        });

        return diagnostics;
    }

    internal static ComplianceContracts.ParameterComplianceStatistics BuildStatistics(
        ParameterExistenceResult? existenceResult,
        SharedParameterValidationResult? sharedParameterResult,
        ParameterValueValidationResult? valueResult,
        IReadOnlyList<ComplianceContracts.ParameterComplianceFinding> findings)
    {
        var objectsChecked = findings
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var objectsFailed = findings
            .Where(finding => !finding.Passed)
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return new ComplianceContracts.ParameterComplianceStatistics
        {
            ObjectsChecked = objectsChecked,
            ObjectsPassed = objectsChecked - objectsFailed,
            ObjectsFailed = objectsFailed,
            ParametersChecked = findings.Count,
            MissingParameters = existenceResult?.Statistics?.MissingParameters ?? 0,
            MissingSharedParameters = sharedParameterResult?.Statistics?.MissingSharedParameters ?? 0,
            InvalidSharedParameters = sharedParameterResult?.Statistics?.InvalidSharedParameters ?? 0,
            MissingValues = valueResult?.Statistics?.MissingValues ?? 0,
            InvalidValues = valueResult?.Statistics?.InvalidValues ?? 0,
            ExistenceChecksRun = existenceResult?.Findings?.Count ?? 0,
            SharedParameterChecksRun = sharedParameterResult?.Findings?.Count ?? 0,
            ValueChecksRun = valueResult?.Findings?.Count ?? 0
        };
    }

    internal static ComplianceContracts.ParameterComplianceSummary BuildSummary(
        IReadOnlyList<ComplianceContracts.ParameterComplianceFinding> findings,
        ComplianceContracts.ParameterComplianceStatistics statistics)
    {
        var passedChecks = findings.Count(finding => finding.Passed);
        var failedChecks = findings.Count - passedChecks;
        var compliancePercentage = findings.Count == 0
            ? 100m
            : Math.Round((decimal)passedChecks / findings.Count * 100m, 2, MidpointRounding.AwayFromZero);

        return new ComplianceContracts.ParameterComplianceSummary
        {
            ObjectsChecked = statistics.ObjectsChecked,
            ParametersChecked = statistics.ParametersChecked,
            PassedChecks = passedChecks,
            FailedChecks = failedChecks,
            CompliancePercentage = compliancePercentage
        };
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        ComplianceContracts.ParameterComplianceRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["complianceOperation"] = "parameter-compliance",
            ["targetSetId"] = request.TargetSet.TargetSetId
        };

        if (!string.IsNullOrWhiteSpace(request.RuleId))
        {
            metadata["ruleId"] = request.RuleId;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata["correlationId"] = request.CorrelationId;
        }

        if (request.SharedParameterFile is not null)
        {
            metadata["sharedParameterFilePath"] = request.SharedParameterFile.FilePath;
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
        ParameterExistenceResult? existenceResult,
        SharedParameterValidationResult? sharedParameterResult,
        ParameterValueValidationResult? valueResult)
    {
        var stages = new List<string>();

        if (existenceResult is not null)
        {
            stages.Add("existence");
        }

        if (sharedParameterResult is not null)
        {
            stages.Add("shared-parameter");
        }

        if (valueResult is not null)
        {
            stages.Add("value");
        }

        var stageList = stages.Count == 0 ? "none" : string.Join(", ", stages);
        return $"Parameter compliance engine '{engineId}' completed stages: {stageList}.";
    }
}
