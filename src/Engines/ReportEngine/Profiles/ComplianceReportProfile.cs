using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Aggregation;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Engines.Report.Profiles;

/// <summary>
/// Prepares compliance report output from evidence and diagnostics.
/// </summary>
public sealed class ComplianceReportProfile : IComplianceReportProfile
{
    public ReportProfileType ProfileType => ReportProfileType.Compliance;

    public ReportProfile Profile { get; } = ComplianceReportProfileDefinition.Create();

    public ReportOutput Prepare(ReportProfileRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var evidenceRecords = request.Evidence?.Records ?? [];
        var diagnosticRecords = request.Diagnostics?.Records ?? [];
        var deduplicatedViolations = ComplianceReportCoordinatorSupport.DeduplicateViolations(evidenceRecords);
        var fixContext = new ComplianceReportCoordinatorSupport.ReportFixContext(
            request.FixEnabled,
            request.ParameterDefaults,
            request.ParameterFillRules,
            request.CategoryFixConfiguration);
        var groupedFindings = ComplianceReportCoordinatorSupport.GroupFindings(deduplicatedViolations, fixContext);
        var objectCounts = ComplianceReportCoordinatorSupport.CountObjects(evidenceRecords, deduplicatedViolations);
        var validationScope = ComplianceReportCoordinatorSupport.BuildValidationScope(
            diagnosticRecords,
            request.ExecutionScope);
        var projectImpact = ComplianceReportCoordinatorSupport.BuildProjectImpact(diagnosticRecords, groupedFindings);
        var businessImpact = ComplianceReportCoordinatorSupport.BuildBusinessImpact(projectImpact);
        var correctionPreview = ComplianceReportCoordinatorSupport.BuildAutomaticCorrectionPreview(
            request.FixEnabled,
            request.ParameterDefaults,
            request.ParameterFillRules,
            request.CategoryFixConfiguration,
            groupedFindings,
            projectImpact);
        var checkedObjects = Math.Max(objectCounts.CheckedObjects, validationScope.TotalObjectsChecked);
        var passedObjects = Math.Max(checkedObjects - objectCounts.FailedObjects, 0);
        var issuesFound = groupedFindings.Count;
        var compliancePercentage = CalculateCompliancePercentage(checkedObjects, passedObjects);
        var generatedAt = request.GeneratedAt ?? DateTimeOffset.UtcNow;
        var resultStatus = issuesFound == 0 ? "Pass" : "Fail";
        var rootCauseSummary = ComplianceReportCoordinatorSupport.BuildOverallRootCauseSummary(
            groupedFindings,
            request.CategoryFixConfiguration);
        var rootCauseNarrative = ComplianceReportCoordinatorSupport.BuildRootCauseNarrative(
            request.RuleName ?? request.ReportTitle,
            resultStatus,
            groupedFindings,
            projectImpact,
            correctionPreview,
            request.CategoryFixConfiguration);

        return new ReportOutput
        {
            ReportId = $"report-{request.RuleId}-{generatedAt:yyyyMMddHHmmss}",
            Title = request.ReportTitle,
            ProfileId = Profile.ProfileId,
            GeneratedAt = generatedAt,
            Metadata = new ReportMetadata
            {
                RuleId = request.RuleId,
                ProfileId = Profile.ProfileId,
                CorrelationId = request.CorrelationId,
                GeneratedBy = "ComplianceReportProfile",
                Properties = new Dictionary<string, string>
                {
                    ["profileType"] = ProfileType.ToString(),
                    ["presentationAudience"] = "Coordinator",
                    ["resultStatus"] = resultStatus
                }
            },
            Sections =
            [
                CreateComplianceSummarySection(
                    request,
                    generatedAt,
                    resultStatus,
                    checkedObjects,
                    passedObjects,
                    objectCounts.FailedObjects,
                    issuesFound,
                    compliancePercentage),
                CreateProjectImpactSection(projectImpact),
                CreateBusinessImpactSection(businessImpact),
                CreateRootCauseSection(rootCauseSummary, rootCauseNarrative, groupedFindings),
                CreateAutomaticCorrectionPreviewSection(correctionPreview),
                CreateValidationScopeSection(validationScope),
                CreateGroupedFindingsSection(groupedFindings),
                CreateRecommendationsSection(groupedFindings),
                CreateEvidenceSection(evidenceRecords, request.Evidence?.CollectionId),
                CreateDiagnosticsSection(diagnosticRecords, request.Diagnostics?.CollectionId)
            ]
        };
    }

    private static decimal CalculateCompliancePercentage(int checkedObjects, int passedObjects)
    {
        if (checkedObjects == 0)
        {
            return 100m;
        }

        return Math.Round((decimal)passedObjects / checkedObjects * 100m, 2);
    }

    private static ReportSection CreateComplianceSummarySection(
        ReportProfileRequest request,
        DateTimeOffset generatedAt,
        string resultStatus,
        int checkedObjects,
        int passedObjects,
        int failedObjects,
        int issuesFound,
        decimal compliancePercentage)
    {
        return new ReportSection
        {
            Name = ComplianceReportProfileSections.ComplianceSummary,
            Description = "Summarizes overall compliance results.",
            Order = 1,
            Required = true,
            Content = new ReportContent
            {
                Text = issuesFound == 0
                    ? $"Validation passed with {compliancePercentage}% compliance."
                    : $"Validation found {issuesFound} issue group(s) across {failedObjects} object(s). Compliance: {compliancePercentage}%.",
                StructuredData = new Dictionary<string, string>
                {
                    ["ruleId"] = request.RuleId,
                    ["ruleName"] = request.RuleName ?? request.ReportTitle,
                    ["executionDate"] = generatedAt.ToString("u"),
                    ["resultStatus"] = resultStatus,
                    ["checkedObjects"] = checkedObjects.ToString(),
                    ["passedObjects"] = passedObjects.ToString(),
                    ["failedObjects"] = failedObjects.ToString(),
                    ["issuesFound"] = issuesFound.ToString(),
                    ["compliancePercentage"] = compliancePercentage.ToString("0.##")
                }
            }
        };
    }

    private static ReportSection CreateProjectImpactSection(
        ComplianceReportCoordinatorSupport.ProjectImpactSummary projectImpact)
    {
        var lines = ComplianceReportCoordinatorSupport.BuildProjectImpactLines(projectImpact);
        var structuredData = ComplianceReportCoordinatorSupport.BuildStructuredLines("projectImpact", lines);

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.ProjectImpact,
            Description = "Shows placed and affected project objects.",
            Order = 2,
            Required = true,
            Content = new ReportContent
            {
                Text = $"{projectImpact.AffectedInstances} placed instance(s) affected.",
                StructuredData = structuredData
            }
        };
    }

    private static ReportSection CreateBusinessImpactSection(
        ComplianceReportCoordinatorSupport.BusinessImpactSummary businessImpact)
    {
        var lines = ComplianceReportCoordinatorSupport.BuildBusinessImpactLines(businessImpact);
        var structuredData = ComplianceReportCoordinatorSupport.BuildStructuredLines("businessImpact", lines);

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.BusinessImpact,
            Description = "Summarizes correction scope in business terms.",
            Order = 3,
            Required = true,
            Content = new ReportContent
            {
                Text = businessImpact.FamiliesRequiringCorrection == 0
                    ? "No families require correction."
                    : $"{businessImpact.FamiliesRequiringCorrection} family/families require correction.",
                StructuredData = structuredData
            }
        };
    }

    private static ReportSection CreateRootCauseSection(
        string rootCauseSummary,
        string rootCauseNarrative,
        IReadOnlyList<ComplianceReportCoordinatorSupport.BusinessFinding> groupedFindings)
    {
        var structuredData = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["narrative"] = rootCauseNarrative,
            ["findingCount"] = groupedFindings.Count.ToString()
        };

        for (var index = 0; index < groupedFindings.Count; index++)
        {
            structuredData[$"finding[{index}].summary"] = groupedFindings[index].RootCauseSummary;
        }

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.RootCause,
            Description = "Explains why validation failed and what correction will do.",
            Order = 4,
            Required = true,
            Content = new ReportContent
            {
                Text = groupedFindings.Count == 0
                    ? "No root cause was identified."
                    : rootCauseSummary,
                StructuredData = structuredData
            }
        };
    }

    private static ReportSection CreateAutomaticCorrectionPreviewSection(
        ComplianceReportCoordinatorSupport.AutomaticCorrectionPreview preview)
    {
        var lines = ComplianceReportCoordinatorSupport.BuildAutomaticCorrectionPreviewLines(preview);
        var structuredData = ComplianceReportCoordinatorSupport.BuildStructuredLines("correctionPreview", lines);
        structuredData["available"] = preview.Available.ToString();

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.AutomaticCorrectionPreview,
            Description = "Preview of deterministic automatic correction.",
            Order = 5,
            Required = false,
            Content = new ReportContent
            {
                Text = preview.Available
                    ? preview.Action ?? "Automatic correction is available."
                    : "Automatic correction is not available for this result.",
                StructuredData = structuredData
            }
        };
    }

    private static ReportSection CreateValidationScopeSection(
        ComplianceReportCoordinatorSupport.ValidationScopeSummary validationScope)
    {
        var scopeLines = ComplianceReportCoordinatorSupport.BuildValidationScopeLines(validationScope);
        var structuredData = new Dictionary<string, string>
        {
            ["modelScope"] = validationScope.ModelScopeLabel,
            ["validationLevel"] = validationScope.ValidationLevel,
            ["whyCountsDiffer"] = validationScope.WhyCountsDiffer,
            ["scopeLineCount"] = scopeLines.Count.ToString()
        };

        for (var index = 0; index < scopeLines.Count; index++)
        {
            structuredData[$"scopeLine[{index}].label"] = scopeLines[index].Label;
            structuredData[$"scopeLine[{index}].value"] = scopeLines[index].Value;
        }

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.ValidationScope,
            Description = "Explains what was validated and why counts may differ from visible objects.",
            Order = 6,
            Required = true,
            Content = new ReportContent
            {
                Text = validationScope.Categories.Count == 0
                    ? "Validation scope information is unavailable for this report."
                    : $"Validation ran at {validationScope.ValidationLevel} level across {validationScope.ModelScopeLabel} scope.",
                StructuredData = structuredData
            }
        };
    }

    private static ReportSection CreateGroupedFindingsSection(IReadOnlyList<ComplianceReportCoordinatorSupport.BusinessFinding> groupedFindings)
    {
        var structuredData = new Dictionary<string, string>
        {
            ["issueGroupCount"] = groupedFindings.Count.ToString()
        };

        for (var index = 0; index < groupedFindings.Count; index++)
        {
            var finding = groupedFindings[index];
            var prefix = $"group[{index}]";
            structuredData[$"{prefix}.issueTitle"] = finding.IssueTitle;
            structuredData[$"{prefix}.issueType"] = finding.IssueType;
            structuredData[$"{prefix}.severity"] = finding.Severity.ToString();
            structuredData[$"{prefix}.count"] = Math.Max(finding.AffectedTypeCount, finding.AffectedInstanceCount).ToString();
            structuredData[$"{prefix}.affectedTypeCount"] = finding.AffectedTypeCount.ToString();
            structuredData[$"{prefix}.affectedInstanceCount"] = finding.AffectedInstanceCount.ToString();
            structuredData[$"{prefix}.validationScope"] = finding.AffectedInstanceCount > 0 && finding.AffectedTypeCount == 0
                ? "instance"
                : "type";
            structuredData[$"{prefix}.affectedFamilyCount"] = finding.AffectedFamilies.Count.ToString();
            structuredData[$"{prefix}.whyFailed"] = finding.WhyFailed;
            structuredData[$"{prefix}.rootCauseSummary"] = finding.RootCauseSummary;
            structuredData[$"{prefix}.fixStepCount"] = finding.FixSteps.Count.ToString();
            structuredData[$"{prefix}.familyGroupCount"] = finding.AffectedFamilies.Count.ToString();

            for (var fixStepIndex = 0; fixStepIndex < finding.FixSteps.Count; fixStepIndex++)
            {
                structuredData[$"{prefix}.fixStep[{fixStepIndex}]"] = finding.FixSteps[fixStepIndex];
            }

            if (!string.IsNullOrWhiteSpace(finding.ParameterName))
            {
                structuredData[$"{prefix}.parameterName"] = finding.ParameterName;
            }

            if (!string.IsNullOrWhiteSpace(finding.Category))
            {
                structuredData[$"{prefix}.category"] = finding.Category;
            }

            for (var familyIndex = 0; familyIndex < finding.AffectedFamilies.Count; familyIndex++)
            {
                var familyImpact = finding.AffectedFamilies[familyIndex];
                var familyPrefix = $"{prefix}.familyGroup[{familyIndex}]";
                structuredData[$"{familyPrefix}.familyName"] = familyImpact.FamilyName;
                structuredData[$"{familyPrefix}.typeCount"] = familyImpact.AffectedTypes.Count.ToString();
                structuredData[$"{familyPrefix}.placedInstanceCount"] = familyImpact.PlacedInstanceCount.ToString();
                structuredData[$"{familyPrefix}.instanceCount"] = familyImpact.AffectedInstances.Count.ToString();

                for (var typeIndex = 0; typeIndex < familyImpact.AffectedTypes.Count; typeIndex++)
                {
                    structuredData[$"{familyPrefix}.type[{typeIndex}]"] = familyImpact.AffectedTypes[typeIndex];
                }

                for (var instanceIndex = 0; instanceIndex < familyImpact.AffectedInstances.Count; instanceIndex++)
                {
                    structuredData[$"{familyPrefix}.instance[{instanceIndex}]"] = familyImpact.AffectedInstances[instanceIndex];
                }
            }
        }

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.GroupedFindings,
            Description = "Groups compliance findings by issue type.",
            Order = 7,
            Required = true,
            Content = new ReportContent
            {
                Text = groupedFindings.Count == 0
                    ? "No compliance issues were detected."
                    : $"{groupedFindings.Count} issue group(s) were detected.",
                StructuredData = structuredData
            }
        };
    }

    private static ReportSection CreateRecommendationsSection(
        IReadOnlyList<ComplianceReportCoordinatorSupport.BusinessFinding> groupedFindings)
    {
        var structuredData = new Dictionary<string, string>
        {
            ["recommendationCount"] = groupedFindings.Count.ToString()
        };

        for (var index = 0; index < groupedFindings.Count; index++)
        {
            structuredData[$"recommendation[{index}]"] = groupedFindings[index].Recommendation;
        }

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.Recommendations,
            Description = "Suggested actions to resolve compliance issues.",
            Order = 8,
            Required = false,
            Content = new ReportContent
            {
                Text = groupedFindings.Count == 0
                    ? "No remediation is required."
                    : "Review the recommendations below and update affected families before re-running validation.",
                StructuredData = structuredData
            }
        };
    }

    private static ReportSection CreateEvidenceSection(
        IReadOnlyList<EvidenceRecord> evidenceRecords,
        string? collectionId)
    {
        var summary = BuildEvidenceSummary(evidenceRecords);
        var groups = BuildEvidenceGroups(evidenceRecords);
        var structuredData = new Dictionary<string, string>
        {
            ["collectionId"] = collectionId ?? string.Empty,
            ["totalEvidence"] = summary.TotalEvidence.ToString(),
            ["groupCount"] = groups.Count.ToString(),
            ["audience"] = "Technical"
        };

        if (summary.Statistics?.Counts is not null)
        {
            foreach (var pair in summary.Statistics.Counts)
            {
                structuredData[$"statistics.severity.{pair.Key}"] = pair.Value.ToString();
            }
        }

        for (var index = 0; index < groups.Count; index++)
        {
            structuredData[$"group[{index}].key"] = groups[index].GroupKey;
            structuredData[$"group[{index}].count"] = groups[index].EvidenceReferences.Count.ToString();
        }

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.Evidence,
            Description = "Technical evidence referenced by the report.",
            Order = 9,
            Required = false,
            Content = new ReportContent
            {
                Text = $"{evidenceRecords.Count} evidence record(s) included in the compliance report.",
                StructuredData = structuredData,
                EvidenceReferences = evidenceRecords
                    .Select(record => new ReportReference
                    {
                        ReferenceType = "Evidence",
                        ReferenceId = record.EvidenceId,
                        Description = record.Message
                    })
                    .ToArray()
            }
        };
    }

    private static ReportSection CreateDiagnosticsSection(
        IReadOnlyList<DiagnosticRecord> diagnosticRecords,
        string? collectionId)
    {
        var statistics = BuildDiagnosticStatistics(diagnosticRecords);
        var structuredData = new Dictionary<string, string>
        {
            ["collectionId"] = collectionId ?? string.Empty,
            ["totalDiagnostics"] = statistics.TotalCount.ToString(),
            ["audience"] = "Technical"
        };

        if (statistics.Counts is not null)
        {
            foreach (var pair in statistics.Counts)
            {
                structuredData[$"statistics.severity.{pair.Key}"] = pair.Value.ToString();
            }
        }

        return new ReportSection
        {
            Name = ComplianceReportProfileSections.Diagnostics,
            Description = "Technical runtime diagnostics related to the report.",
            Order = 10,
            Required = false,
            Content = new ReportContent
            {
                Text = diagnosticRecords.Count == 0
                    ? "No diagnostics were included."
                    : $"{diagnosticRecords.Count} diagnostic record(s) included.",
                StructuredData = structuredData,
                DiagnosticReferences = diagnosticRecords
                    .Select(record => new ReportReference
                    {
                        ReferenceType = "Diagnostic",
                        ReferenceId = record.DiagnosticId,
                        Description = record.Message
                    })
                    .ToArray()
            }
        };
    }

    private static EvidenceSummary BuildEvidenceSummary(IReadOnlyList<EvidenceRecord> evidenceRecords)
    {
        return new EvidenceSummary
        {
            TotalEvidence = evidenceRecords.Count,
            BySeverity = evidenceRecords
                .GroupBy(record => record.Severity)
                .ToDictionary(group => group.Key, group => group.Count()),
            ByCategory = evidenceRecords
                .GroupBy(record => record.Category)
                .ToDictionary(group => group.Key, group => group.Count()),
            BySource = evidenceRecords
                .GroupBy(record => record.Source.EngineId)
                .ToDictionary(group => group.Key, group => group.Count()),
            ByTarget = evidenceRecords
                .GroupBy(record => record.Target?.TargetId ?? record.Target?.TargetName ?? "unknown")
                .ToDictionary(group => group.Key, group => group.Count()),
            Statistics = BuildEvidenceStatistics(evidenceRecords)
        };
    }

    private static EvidenceStatistics BuildEvidenceStatistics(IReadOnlyList<EvidenceRecord> evidenceRecords)
    {
        var severityCounts = evidenceRecords
            .GroupBy(record => record.Severity.ToString())
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var percentages = evidenceRecords.Count == 0
            ? new Dictionary<string, decimal>()
            : severityCounts.ToDictionary(
                pair => pair.Key,
                pair => Math.Round((decimal)pair.Value / evidenceRecords.Count * 100m, 2));

        return new EvidenceStatistics
        {
            TotalCount = evidenceRecords.Count,
            Counts = severityCounts,
            Totals = new Dictionary<string, int> { ["records"] = evidenceRecords.Count },
            Breakdowns = new Dictionary<string, IReadOnlyDictionary<string, int>>
            {
                ["severity"] = severityCounts
            },
            Percentages = percentages
        };
    }

    private static IReadOnlyList<EvidenceGroup> BuildEvidenceGroups(IReadOnlyList<EvidenceRecord> evidenceRecords)
    {
        return evidenceRecords
            .GroupBy(record => record.Severity)
            .Select(group => new EvidenceGroup
            {
                GroupKey = $"severity:{group.Key}",
                GroupName = group.Key.ToString(),
                EvidenceReferences = group.Select(record => record.EvidenceId).ToArray(),
                Summary = new EvidenceSummary
                {
                    TotalEvidence = group.Count(),
                    BySeverity = new Dictionary<EvidenceSeverity, int> { [group.Key] = group.Count() }
                }
            })
            .OrderBy(group => group.GroupKey, StringComparer.Ordinal)
            .ToArray();
    }

    private static EvidenceStatistics BuildDiagnosticStatistics(IReadOnlyList<DiagnosticRecord> diagnosticRecords)
    {
        var severityCounts = diagnosticRecords
            .GroupBy(record => record.Severity.ToString())
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return new EvidenceStatistics
        {
            TotalCount = diagnosticRecords.Count,
            Counts = severityCounts,
            Breakdowns = new Dictionary<string, IReadOnlyDictionary<string, int>>
            {
                ["severity"] = severityCounts
            }
        };
    }
}
