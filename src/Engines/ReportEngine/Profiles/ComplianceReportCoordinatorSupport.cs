using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Engines.Report.Profiles;

internal static class ComplianceReportCoordinatorSupport
{
    internal const string ValidationScopeCategoryCode = "ValidationScope.Category";

    internal sealed record ReportFixContext(
        bool FixEnabled,
        IReadOnlyDictionary<string, string>? ParameterDefaults,
        IReadOnlyDictionary<string, string>? ParameterFillRules,
        IReadOnlyDictionary<string, ReportCategoryFixConfiguration>? CategoryFixConfiguration);

    internal sealed record BusinessFamilyImpact(
        string FamilyName,
        IReadOnlyList<string> AffectedTypes,
        IReadOnlyList<string> AffectedInstances,
        int PlacedInstanceCount);

    internal sealed record ProjectImpactSummary(
        string CategoryLabel,
        int PlacedInstances,
        int AffectedInstances,
        int AffectedTypes,
        int AffectedFamilies);

    internal sealed record BusinessImpactSummary(
        int FamiliesRequiringCorrection,
        int TypesAffected,
        int PlacedInstancesAffected,
        string CategoryLabel);

    internal sealed record AutomaticCorrectionPreview(
        bool Available,
        string? Action,
        string? DefaultValue,
        int AffectedTypes,
        int AffectedInstances,
        int ExpectedTypesCorrected,
        int ExpectedInstancesCompliant);

    internal sealed record ValidationScopeCategory(
        string CategoryName,
        string ValidationLevel,
        int FamiliesChecked,
        int TypesChecked,
        int ObjectsChecked,
        int InstancesChecked);

    internal sealed record ValidationScopeSummary(
        string ModelScopeLabel,
        IReadOnlyList<ValidationScopeCategory> Categories,
        string ValidationLevel,
        string WhyCountsDiffer,
        int TotalObjectsChecked);
    internal sealed record BusinessFinding(
        string IssueKey,
        string IssueTitle,
        string IssueType,
        string? ParameterName,
        string? Category,
        string WhyFailed,
        string RootCauseSummary,
        IReadOnlyList<string> FixSteps,
        string Recommendation,
        EvidenceSeverity Severity,
        IReadOnlyList<BusinessFamilyImpact> AffectedFamilies,
        int AffectedTypeCount,
        int AffectedInstanceCount);

    internal static IReadOnlyList<EvidenceRecord> DeduplicateViolations(IReadOnlyList<EvidenceRecord> evidenceRecords)
    {
        var violations = evidenceRecords
            .Where(record => record.Severity is EvidenceSeverity.Error or EvidenceSeverity.Critical or EvidenceSeverity.Warning)
            .ToArray();

        var selected = new Dictionary<string, EvidenceRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in violations.OrderBy(record => GetEvidencePriority(record)))
        {
            var key = BuildBusinessKey(record);
            if (!selected.ContainsKey(key))
            {
                selected[key] = NormalizeViolationRecord(record);
            }
        }

        return selected.Values
            .OrderBy(record => record.Target?.TargetName ?? record.Target?.TargetId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.Message, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<BusinessFinding> GroupFindings(
        IReadOnlyList<EvidenceRecord> deduplicatedViolations,
        ReportFixContext? fixContext = null)
    {
        return deduplicatedViolations
            .GroupBy(BuildIssueGroupKey, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var sample = group.First();
                var parameterName = TryGetStructuredValue(sample, "parameterName");
                var issueType = ResolveIssueType(sample, parameterName);
                var issueTitle = BuildGroupedIssueTitle(parameterName, issueType);
                var category = ResolveCategory(sample);
                var isInstanceScope = group.Any(IsInstanceScopedEvidence);
                var familyImpacts = BuildFamilyImpacts(group, isInstanceScope);
                var affectedTypeCount = isInstanceScope
                    ? 0
                    : familyImpacts.Sum(impact => impact.AffectedTypes.Count);
                var affectedInstanceCount = isInstanceScope
                    ? familyImpacts.Sum(impact => impact.AffectedInstances.Count)
                    : familyImpacts.Sum(impact => impact.PlacedInstanceCount);
                var fixSteps = BuildFixSteps(
                    issueType,
                    parameterName,
                    familyImpacts,
                    isInstanceScope,
                    category,
                    fixContext);
                var whyFailed = BuildWhyFailed(issueType, parameterName, sample, isInstanceScope);
                var rootCauseSummary = BuildRootCauseSummary(
                    parameterName,
                    affectedTypeCount,
                    affectedInstanceCount,
                    familyImpacts.Count,
                    category,
                    isInstanceScope);

                return new BusinessFinding(
                    IssueKey: group.Key,
                    IssueTitle: issueTitle,
                    IssueType: issueType,
                    ParameterName: parameterName,
                    Category: category,
                    WhyFailed: whyFailed,
                    RootCauseSummary: rootCauseSummary,
                    FixSteps: fixSteps,
                    Recommendation: FormatRecommendation(issueTitle, fixSteps),
                    Severity: group.Max(record => record.Severity),
                    AffectedFamilies: familyImpacts,
                    AffectedTypeCount: affectedTypeCount,
                    AffectedInstanceCount: affectedInstanceCount);
            })
            .OrderByDescending(finding => finding.Severity)
            .ThenBy(finding => finding.IssueTitle, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static (int CheckedObjects, int FailedObjects, int PassedObjects) CountObjects(
        IReadOnlyList<EvidenceRecord> evidenceRecords,
        IReadOnlyList<EvidenceRecord> deduplicatedViolations)
    {
        var checkedObjects = evidenceRecords
            .Where(record => record.Target?.TargetId is not null)
            .Select(record => record.Target!.TargetId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var failedObjects = deduplicatedViolations
            .Where(record => record.Target?.TargetId is not null)
            .Select(record => record.Target!.TargetId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (checkedObjects == 0 && failedObjects > 0)
        {
            checkedObjects = failedObjects;
        }

        var passedObjects = Math.Max(checkedObjects - failedObjects, 0);
        return (checkedObjects, failedObjects, passedObjects);
    }

    internal static ValidationScopeSummary BuildValidationScope(
        IReadOnlyList<DiagnosticRecord> diagnostics,
        ExecutionScope? executionScope)
    {
        var categories = diagnostics
            .Where(record => string.Equals(record.Source.Code, ValidationScopeCategoryCode, StringComparison.Ordinal))
            .Select(ParseValidationScopeCategory)
            .Where(category => category is not null)
            .Cast<ValidationScopeCategory>()
            .OrderBy(category => category.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var validationLevel = ResolveOverallValidationLevel(categories);
        var totalObjectsChecked = categories.Sum(category => category.ObjectsChecked);

        return new ValidationScopeSummary(
            ModelScopeLabel: ResolveModelScopeLabel(executionScope),
            Categories: categories,
            ValidationLevel: validationLevel,
            WhyCountsDiffer: BuildWhyCountsDiffer(categories, validationLevel),
            TotalObjectsChecked: totalObjectsChecked);
    }

    private static ValidationScopeCategory? ParseValidationScopeCategory(DiagnosticRecord record)
    {
        if (record.StructuredMetadata is null)
        {
            return null;
        }

        if (!record.StructuredMetadata.TryGetValue("scopeCategory", out var categoryName)
            || string.IsNullOrWhiteSpace(categoryName))
        {
            return null;
        }

        return new ValidationScopeCategory(
            CategoryName: categoryName,
            ValidationLevel: record.StructuredMetadata.GetValueOrDefault("validationLevel") ?? "Project",
            FamiliesChecked: ParseCount(record.StructuredMetadata, "familiesChecked"),
            TypesChecked: ParseCount(record.StructuredMetadata, "typesChecked"),
            ObjectsChecked: ParseCount(record.StructuredMetadata, "objectsChecked"),
            InstancesChecked: ParseCount(record.StructuredMetadata, "instancesChecked"));
    }

    private static int ParseCount(IReadOnlyDictionary<string, string> metadata, string key)
    {
        return int.TryParse(metadata.GetValueOrDefault(key), out var count) ? count : 0;
    }

    private static string ResolveModelScopeLabel(ExecutionScope? executionScope)
    {
        if (executionScope is null)
        {
            return "Project";
        }

        return executionScope.ScopeType switch
        {
            "EntireModel" => "Project",
            "Category" => "Category",
            "Selection" => "Selection",
            "Family" => "Family",
            "Type" => "Type",
            "Instance" => "Instance",
            _ => executionScope.ScopeType
        };
    }

    private static string ResolveOverallValidationLevel(IReadOnlyList<ValidationScopeCategory> categories)
    {
        if (categories.Count == 0)
        {
            return "Project";
        }

        var distinctLevels = categories
            .Select(category => category.ValidationLevel)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return distinctLevels.Length == 1 ? distinctLevels[0] : "Mixed";
    }

    private static string BuildWhyCountsDiffer(
        IReadOnlyList<ValidationScopeCategory> categories,
        string validationLevel)
    {
        if (categories.Count == 0)
        {
            return "This report summarizes what the validation rule checked in the model. Checked counts come from loaded families and types, not from what is visible in the current view.";
        }

        if (string.Equals(validationLevel, "Type", StringComparison.OrdinalIgnoreCase))
        {
            if (categories.Count == 1)
            {
                var category = ToSingularCategoryLabel(categories[0].CategoryName);
                return
                    $"This rule validates {category} family types in the project, not individual placed instances. " +
                    $"If you see fewer {category.ToLowerInvariant()} instances in the model than types checked, that is expected. Multiple instances can share one type, and types without placed instances are still checked.";
            }

            return
                "This rule validates family types in the project, not individual placed instances. " +
                "If visible instance counts differ from checked counts, that is expected because many instances share one type and unloaded or unplaced types are still checked.";
        }

        if (string.Equals(validationLevel, "Family", StringComparison.OrdinalIgnoreCase))
        {
            return
                "This rule validates whole families. Each family can contain multiple types, so checked family counts may differ from what you see selected in a view.";
        }

        if (string.Equals(validationLevel, "Instance", StringComparison.OrdinalIgnoreCase))
        {
            return
                "This rule validates placed instances. Checked counts reflect individual elements rather than shared family types.";
        }

        return
            "Checked counts describe what the rule evaluated in the model. They may differ from visible selection counts when validation runs at project or category scope.";
    }

    private static string ToSingularCategoryLabel(string categoryName)
    {
        if (categoryName.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
        {
            return categoryName[..^3] + "y";
        }

        if (categoryName.EndsWith('s') && categoryName.Length > 1)
        {
            return categoryName[..^1];
        }

        return categoryName;
    }

    internal static IReadOnlyList<(string Label, string Value)> BuildValidationScopeLines(ValidationScopeSummary scope)
    {
        var lines = new List<(string Label, string Value)>
        {
            ("Model Scope", scope.ModelScopeLabel)
        };

        foreach (var category in scope.Categories)
        {
            var singular = ToSingularCategoryLabel(category.CategoryName);

            if (category.InstancesChecked > 0
                && string.Equals(category.ValidationLevel, "Instance", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(($"{singular} Instances Checked", category.InstancesChecked.ToString()));
            }

            if (category.TypesChecked > 0)
            {
                lines.Add(($"{singular} Types Checked", category.TypesChecked.ToString()));
            }

            if (category.FamiliesChecked > 0)
            {
                lines.Add(($"{singular} Families Checked", category.FamiliesChecked.ToString()));
            }

            if (category.ObjectsChecked > 0
                && category.ObjectsChecked != category.TypesChecked
                && category.ObjectsChecked != category.FamiliesChecked)
            {
                lines.Add(($"{singular} Objects Checked", category.ObjectsChecked.ToString()));
            }
        }

        lines.Add(("Validation Level", scope.ValidationLevel));
        return lines;
    }

    private static string BuildIssueGroupKey(EvidenceRecord record)
    {
        var parameterName = TryGetStructuredValue(record, "parameterName");
        if (!string.IsNullOrWhiteSpace(parameterName))
        {
            var issueType = ResolveIssueType(record, parameterName);
            return $"{issueType}|{parameterName}".ToLowerInvariant();
        }

        if (ContainsAny(record.Message, "imported cad", "import cad"))
        {
            return "imported-cad";
        }

        if (IsNamingEvidence(record))
        {
            return "naming-violation";
        }

        return $"issue|{NormalizeMessage(record.Message)}".ToLowerInvariant();
    }

    private static string BuildGroupedIssueTitle(string? parameterName, string issueType)
    {
        return issueType switch
        {
            "MissingParameter" => $"Missing {parameterName}",
            "MissingSharedParameter" => $"Missing {parameterName}",
            "InvalidSharedParameter" => $"Invalid shared parameter {parameterName}",
            "MissingValue" => $"{parameterName} missing value",
            "ImportedCad" => "Imported CAD detected",
            "NamingViolation" => "Naming standard not met",
            _ => "Compliance issue"
        };
    }

    private static string BuildBusinessKey(EvidenceRecord record)
    {
        var targetId = record.Target?.TargetId ?? record.Target?.TargetName ?? "unknown";
        var parameterName = TryGetStructuredValue(record, "parameterName");

        if (!string.IsNullOrWhiteSpace(parameterName))
        {
            return $"{targetId}|parameter|{parameterName}".ToLowerInvariant();
        }

        if (ContainsAny(record.Message, "imported cad", "import cad"))
        {
            return $"{targetId}|imported-cad".ToLowerInvariant();
        }

        if (IsNamingEvidence(record))
        {
            return $"{targetId}|naming".ToLowerInvariant();
        }

        return $"{targetId}|message|{NormalizeMessage(record.Message)}".ToLowerInvariant();
    }

    private static bool IsNamingEvidence(EvidenceRecord record)
    {
        var evidenceId = record.EvidenceId.ToLowerInvariant();
        return evidenceId.Contains("naming-prefix", StringComparison.Ordinal)
            || evidenceId.Contains("naming-pattern", StringComparison.Ordinal)
            || evidenceId.StartsWith("prefix-", StringComparison.Ordinal)
            || ContainsAny(record.Message, "prefix", "naming pattern", "forbidden characters");
    }

    private static int GetEvidencePriority(EvidenceRecord record)
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

        if (evidenceId.Contains("naming-prefix", StringComparison.Ordinal)
            || evidenceId.StartsWith("prefix-", StringComparison.Ordinal))
        {
            return 3;
        }

        if (evidenceId.Contains("naming-pattern", StringComparison.Ordinal))
        {
            return 4;
        }

        return 5;
    }

    private static EvidenceRecord NormalizeViolationRecord(EvidenceRecord record)
    {
        var parameterName = TryGetStructuredValue(record, "parameterName");
        var issueType = ResolveIssueType(record, parameterName);
        var targetName = ResolveBusinessDisplayName(record);
        var message = BuildIssueTitle(record, parameterName, issueType);

        return record with
        {
            Message = $"{targetName}: {message}"
        };
    }

    private static string ResolveIssueType(EvidenceRecord record, string? parameterName)
    {
        var evidenceId = record.EvidenceId.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(parameterName))
        {
            if (evidenceId.Contains("shared-parameter", StringComparison.Ordinal))
            {
                if (ContainsAny(record.Message, "not a shared parameter", "definition mismatch", "does not match"))
                {
                    return "InvalidSharedParameter";
                }

                return "MissingSharedParameter";
            }

            if (evidenceId.Contains("parameter-value", StringComparison.Ordinal)
                || ContainsAny(record.Message, "missing a required value", "missing value", "empty"))
            {
                return "MissingValue";
            }

            return "MissingParameter";
        }

        if (ContainsAny(record.Message, "imported cad", "import cad"))
        {
            return "ImportedCad";
        }

        if (IsNamingEvidence(record))
        {
            return "NamingViolation";
        }

        return "ComplianceIssue";
    }

    private static string BuildIssueTitle(EvidenceRecord record, string? parameterName, string issueType)
    {
        return issueType switch
        {
            "MissingParameter" => $"{parameterName} missing",
            "MissingSharedParameter" => $"{parameterName} missing",
            "InvalidSharedParameter" => $"{parameterName} invalid",
            "MissingValue" => $"{parameterName} missing value",
            "ImportedCad" => "Imported CAD detected",
            "NamingViolation" => "Naming standard not met",
            _ => NormalizeMessage(record.Message)
        };
    }

    private static string? ResolveCategory(EvidenceRecord record)
    {
        var category = TryGetStructuredValue(record, "categoryName")
            ?? TryGetStructuredValue(record, "category");
        if (!string.IsNullOrWhiteSpace(category) && !IsInternalIdentifier(category))
        {
            return category;
        }

        var targetSetDescription = record.Target?.TargetSetDescription;
        if (!string.IsNullOrWhiteSpace(targetSetDescription) && !IsInternalIdentifier(targetSetDescription))
        {
            return targetSetDescription;
        }

        return null;
    }

    internal static ProjectImpactSummary BuildProjectImpact(
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<BusinessFinding> groupedFindings)
    {
        var scopeCategories = diagnostics
            .Where(record => string.Equals(record.Source.Code, ValidationScopeCategoryCode, StringComparison.Ordinal))
            .Select(ParseValidationScopeCategory)
            .Where(category => category is not null)
            .Cast<ValidationScopeCategory>()
            .ToArray();

        var placedInstances = scopeCategories.Length == 0
            ? 0
            : scopeCategories.Sum(category => ParseScopePlacedInstances(diagnostics, category.CategoryName));

        var affectedTypes = groupedFindings.Sum(finding => finding.AffectedTypeCount);
        var affectedFamilies = groupedFindings
            .SelectMany(finding => finding.AffectedFamilies)
            .Select(impact => impact.FamilyName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var affectedInstances = groupedFindings.Sum(finding => finding.AffectedInstanceCount);

        return new ProjectImpactSummary(
            CategoryLabel: "Element",
            PlacedInstances: placedInstances,
            AffectedInstances: affectedInstances,
            AffectedTypes: affectedTypes,
            AffectedFamilies: affectedFamilies);
    }

    internal static BusinessImpactSummary BuildBusinessImpact(ProjectImpactSummary projectImpact)
    {
        return new BusinessImpactSummary(
            FamiliesRequiringCorrection: projectImpact.AffectedFamilies,
            TypesAffected: projectImpact.AffectedTypes,
            PlacedInstancesAffected: projectImpact.AffectedInstances,
            CategoryLabel: projectImpact.CategoryLabel);
    }

    internal static AutomaticCorrectionPreview BuildAutomaticCorrectionPreview(
        bool fixEnabled,
        IReadOnlyDictionary<string, string>? parameterDefaults,
        IReadOnlyDictionary<string, string>? parameterFillRules,
        IReadOnlyDictionary<string, ReportCategoryFixConfiguration>? categoryFixConfiguration,
        IReadOnlyList<BusinessFinding> groupedFindings,
        ProjectImpactSummary projectImpact)
    {
        if (!fixEnabled || groupedFindings.Count == 0)
        {
            return new AutomaticCorrectionPreview(
                Available: false,
                Action: null,
                DefaultValue: null,
                AffectedTypes: 0,
                AffectedInstances: 0,
                ExpectedTypesCorrected: 0,
                ExpectedInstancesCompliant: 0);
        }

        var actions = BuildAutomaticCorrectionActions(
            parameterDefaults,
            parameterFillRules,
            categoryFixConfiguration,
            groupedFindings);

        var action = actions.Count == 0
            ? "Apply configured automatic correction."
            : string.Join("; ", actions);

        var defaultValue = BuildAutomaticCorrectionDefaultValueSummary(
            parameterDefaults,
            parameterFillRules,
            groupedFindings);

        return new AutomaticCorrectionPreview(
            Available: true,
            Action: action,
            DefaultValue: defaultValue,
            AffectedTypes: projectImpact.AffectedTypes,
            AffectedInstances: projectImpact.AffectedInstances,
            ExpectedTypesCorrected: projectImpact.AffectedTypes,
            ExpectedInstancesCompliant: projectImpact.AffectedInstances);
    }

    internal static string BuildOverallRootCauseSummary(
        IReadOnlyList<BusinessFinding> groupedFindings,
        IReadOnlyDictionary<string, ReportCategoryFixConfiguration>? categoryFixConfiguration = null)
    {
        if (groupedFindings.Count == 0)
        {
            return "No root cause was identified.";
        }

        var parts = new List<string>();
        var parameterFindings = groupedFindings
            .Where(finding => !string.IsNullOrWhiteSpace(finding.ParameterName))
            .ToArray();
        var namingFindings = groupedFindings
            .Where(finding => finding.IssueType == "NamingViolation")
            .ToArray();

        if (parameterFindings.Length > 0)
        {
            parts.Add(BuildParameterRootCauseSummary(parameterFindings));
        }

        if (namingFindings.Length > 0)
        {
            parts.Add(BuildNamingRootCauseSummary(namingFindings, categoryFixConfiguration));
        }

        if (parts.Count > 0)
        {
            return string.Join(" ", parts);
        }

        var totalTypes = groupedFindings.Sum(finding => finding.AffectedTypeCount);
        var totalFamilies = groupedFindings
            .SelectMany(finding => finding.AffectedFamilies)
            .Select(impact => impact.FamilyName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return groupedFindings.Count == 1
            ? $"1 issue affects {totalTypes} family type(s) across {totalFamilies} family/families."
            : $"{groupedFindings.Count} issues affect {totalTypes} family type(s) across {totalFamilies} family/families.";
    }

    private static string BuildParameterRootCauseSummary(IReadOnlyList<BusinessFinding> parameterFindings)
    {
        var parameterNames = parameterFindings
            .Select(finding => finding.ParameterName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (parameterNames.Length == 1)
        {
            var parameterName = parameterNames[0]!;
            var totalTypes = parameterFindings.Sum(finding => finding.AffectedTypeCount);
            var totalInstances = parameterFindings.Sum(finding => finding.AffectedInstanceCount);
            var totalFamilies = parameterFindings
                .SelectMany(finding => finding.AffectedFamilies)
                .Select(impact => impact.FamilyName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            if (totalInstances > 0 && totalTypes == 0)
            {
                return $"Required parameter {parameterName} is missing a value on {totalInstances} placed instance(s) across {totalFamilies} family/families.";
            }

            var hasMissingParameter = parameterFindings.Any(finding =>
                finding.IssueType is "MissingParameter" or "MissingSharedParameter");
            var hasMissingValue = parameterFindings.Any(finding => finding.IssueType == "MissingValue");

            if (hasMissingParameter && !hasMissingValue)
            {
                return $"Required parameter {parameterName} is missing on {totalTypes} family type(s) across {totalFamilies} family/families.";
            }

            if (hasMissingValue && !hasMissingParameter)
            {
                return $"Required parameter {parameterName} is empty on {totalTypes} family type(s) across {totalFamilies} family/families.";
            }

            return $"Required parameter {parameterName} needs correction on {totalTypes} family type(s) across {totalFamilies} family/families.";
        }

        var summaryParts = parameterFindings
            .Select(finding =>
            {
                if (finding.AffectedInstanceCount > 0 && finding.AffectedTypeCount == 0)
                {
                    return $"{finding.ParameterName} on {finding.AffectedInstanceCount} placed instance(s)";
                }

                return $"{finding.ParameterName} on {finding.AffectedTypeCount} family type(s)";
            })
            .ToArray();

        return $"{parameterFindings.Count} required parameters need correction: {string.Join("; ", summaryParts)}.";
    }

    private static string BuildNamingRootCauseSummary(
        IReadOnlyList<BusinessFinding> namingFindings,
        IReadOnlyDictionary<string, ReportCategoryFixConfiguration>? categoryFixConfiguration)
    {
        var totalTypes = namingFindings.Sum(finding => finding.AffectedTypeCount);
        var categories = namingFindings
            .Select(finding => finding.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var prefixLabels = categories
            .Select(category =>
            {
                if (categoryFixConfiguration?.TryGetValue(category!, out var configuration) == true
                    && !string.IsNullOrWhiteSpace(configuration.RequiredPrefix))
                {
                    return configuration.RequiredPrefix;
                }

                return null;
            })
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (prefixLabels.Length == 1)
        {
            return $"{totalTypes} family type(s) are missing the {prefixLabels[0]} prefix.";
        }

        return $"{totalTypes} family type(s) do not meet the required naming prefix.";
    }

    private static IReadOnlyList<string> BuildAutomaticCorrectionActions(
        IReadOnlyDictionary<string, string>? parameterDefaults,
        IReadOnlyDictionary<string, string>? parameterFillRules,
        IReadOnlyDictionary<string, ReportCategoryFixConfiguration>? categoryFixConfiguration,
        IReadOnlyList<BusinessFinding> groupedFindings)
    {
        var actions = new List<string>();
        var parameterIssueTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MissingParameter",
            "MissingSharedParameter",
            "MissingValue"
        };

        foreach (var parameterName in groupedFindings
                     .Where(finding => parameterIssueTypes.Contains(finding.IssueType))
                     .Select(finding => finding.ParameterName)
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
        {
            var findings = groupedFindings
                .Where(finding => string.Equals(finding.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var hasMissingParameter = findings.Any(finding =>
                finding.IssueType is "MissingParameter" or "MissingSharedParameter");
            var hasMissingValue = findings.Any(finding => finding.IssueType == "MissingValue");

            if (hasMissingValue
                && parameterFillRules?.TryGetValue(parameterName!, out var fillRule) == true)
            {
                actions.Add($"Fill {parameterName} from {DescribeFillSource(fillRule)}");
            }
            else if (parameterDefaults?.TryGetValue(parameterName!, out var configuredDefault) == true)
            {
                actions.Add(hasMissingParameter
                    ? $"Create {parameterName} with default {configuredDefault}"
                    : $"Set {parameterName} to {configuredDefault}");
            }
            else if (hasMissingParameter)
            {
                actions.Add($"Create parameter {parameterName}");
            }
            else if (hasMissingValue)
            {
                actions.Add($"Set values for {parameterName}");
            }
        }

        foreach (var finding in groupedFindings.Where(finding => finding.IssueType == "NamingViolation"))
        {
            if (string.IsNullOrWhiteSpace(finding.Category)
                || categoryFixConfiguration?.TryGetValue(finding.Category, out var configuration) != true
                || configuration.PrefixFixScope == PrefixFixScope.None
                || string.IsNullOrWhiteSpace(configuration.RequiredPrefix))
            {
                continue;
            }

            var renameScope = configuration.PrefixFixScope.HasFlag(PrefixFixScope.Type)
                && configuration.PrefixFixScope.HasFlag(PrefixFixScope.Family)
                    ? "types and families"
                    : configuration.PrefixFixScope.HasFlag(PrefixFixScope.Type)
                        ? "types"
                        : "families";

            var action = $"Rename {finding.Category} {renameScope} to add {configuration.RequiredPrefix} prefix";
            if (!actions.Contains(action, StringComparer.OrdinalIgnoreCase))
            {
                actions.Add(action);
            }
        }

        return actions;
    }

    private static string? BuildAutomaticCorrectionDefaultValueSummary(
        IReadOnlyDictionary<string, string>? parameterDefaults,
        IReadOnlyDictionary<string, string>? parameterFillRules,
        IReadOnlyList<BusinessFinding> groupedFindings)
    {
        var summaries = groupedFindings
            .Where(finding => !string.IsNullOrWhiteSpace(finding.ParameterName))
            .Select(finding => finding.ParameterName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(parameterName =>
            {
                if (parameterFillRules?.TryGetValue(parameterName, out var fillRule) == true)
                {
                    return $"{parameterName}={fillRule}";
                }

                if (parameterDefaults?.TryGetValue(parameterName, out var configuredDefault) == true)
                {
                    return $"{parameterName}={configuredDefault}";
                }

                return null;
            })
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .ToArray();

        return summaries.Length == 0 ? null : string.Join("; ", summaries);
    }

    private static string DescribeFillSource(string fillRule)
    {
        if (!fillRule.StartsWith("from:", StringComparison.OrdinalIgnoreCase))
        {
            return fillRule;
        }

        var source = fillRule["from:".Length..].Trim();
        return source switch
        {
            "FamilyTypeName" => "family type name",
            "FamilyName" => "family name",
            _ => $"parameter {source}"
        };
    }

    internal static string BuildRootCauseNarrative(
        string ruleName,
        string resultStatus,
        IReadOnlyList<BusinessFinding> groupedFindings,
        ProjectImpactSummary projectImpact,
        AutomaticCorrectionPreview correctionPreview,
        IReadOnlyDictionary<string, ReportCategoryFixConfiguration>? categoryFixConfiguration = null)
    {
        if (groupedFindings.Count == 0)
        {
            return $"Rule {ruleName} passed validation.";
        }

        return BuildOverallRootCauseSummary(groupedFindings, categoryFixConfiguration);
    }

    internal static bool IsInternalIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith("target-set-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.StartsWith("corr-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return value.Contains("target-set-", StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolveBusinessDisplayName(EvidenceRecord record)
    {
        var typeName = TryGetStructuredValue(record, "typeName");
        if (!string.IsNullOrWhiteSpace(typeName) && !IsInternalIdentifier(typeName))
        {
            return typeName;
        }

        var targetName = record.Target?.TargetName;
        if (!string.IsNullOrWhiteSpace(targetName) && !IsInternalIdentifier(targetName))
        {
            return targetName;
        }

        return "Unknown object";
    }

    private static int ParseScopePlacedInstances(IReadOnlyList<DiagnosticRecord> diagnostics, string categoryName)
    {
        foreach (var record in diagnostics)
        {
            if (!string.Equals(record.Source.Code, ValidationScopeCategoryCode, StringComparison.Ordinal))
            {
                continue;
            }

            if (record.StructuredMetadata?.GetValueOrDefault("scopeCategory") != categoryName)
            {
                continue;
            }

            return ParseCount(record.StructuredMetadata, "placedInstances");
        }

        return 0;
    }

    private static IReadOnlyList<BusinessFamilyImpact> BuildFamilyImpacts(
        IEnumerable<EvidenceRecord> records,
        bool instanceScope)
    {
        return records
            .GroupBy(record => TryGetStructuredValue(record, "familyName")
                ?? ResolveBusinessDisplayName(record), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                if (instanceScope)
                {
                    var instances = group
                        .Select(ResolveInstanceDisplayName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    var types = group
                        .Select(record => TryGetStructuredValue(record, "typeName"))
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(name => name!, StringComparer.OrdinalIgnoreCase)
                        .Cast<string>()
                        .ToArray();

                    return new BusinessFamilyImpact(
                        FamilyName: group.Key,
                        AffectedTypes: types,
                        AffectedInstances: instances,
                        PlacedInstanceCount: instances.Length);
                }

                var affectedTypes = group
                    .Select(ResolveBusinessDisplayName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var placedInstances = group
                    .Select(record => int.TryParse(TryGetStructuredValue(record, "placedInstanceCount"), out var count) ? count : 0)
                    .Sum();

                return new BusinessFamilyImpact(
                    FamilyName: group.Key,
                    AffectedTypes: affectedTypes,
                    AffectedInstances: [],
                    PlacedInstanceCount: placedInstances);
            })
            .OrderBy(impact => impact.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsInstanceScopedEvidence(EvidenceRecord record)
    {
        var objectKind = TryGetStructuredValue(record, "objectKind");
        if (string.Equals(objectKind, "familyInstance", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var validationScope = TryGetStructuredValue(record, "validationScope");
        return string.Equals(validationScope, "instance", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveInstanceDisplayName(EvidenceRecord record)
    {
        var targetName = record.Target?.TargetName;
        if (!string.IsNullOrWhiteSpace(targetName) && !IsInternalIdentifier(targetName))
        {
            return targetName;
        }

        var typeName = TryGetStructuredValue(record, "typeName");
        if (!string.IsNullOrWhiteSpace(typeName) && !IsInternalIdentifier(typeName))
        {
            return typeName;
        }

        var targetId = record.Target?.TargetId;
        return !string.IsNullOrWhiteSpace(targetId)
            ? $"Instance {targetId}"
            : "Unknown instance";
    }

    private static string BuildRootCauseSummary(
        string? parameterName,
        int affectedTypeCount,
        int affectedInstanceCount,
        int affectedFamilyCount,
        string? category,
        bool instanceScope)
    {
        if (instanceScope)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return $"{affectedInstanceCount} placed instance(s) from {affectedFamilyCount} family/families require correction.";
            }

            return $"{affectedInstanceCount} placed instance(s) from {affectedFamilyCount} family/families are missing a value for {parameterName}.";
        }

        var categoryLabel = string.IsNullOrWhiteSpace(category)
            ? "family"
            : ToSingularCategoryLabel(category).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return $"{affectedTypeCount} {categoryLabel} type(s) from {affectedFamilyCount} family/families require correction.";
        }

        return $"{affectedTypeCount} family type(s) from {affectedFamilyCount} family/families are missing {parameterName}.";
    }

    private static IReadOnlyList<string> BuildFixSteps(
        string issueType,
        string? parameterName,
        IReadOnlyList<BusinessFamilyImpact> familyImpacts,
        bool instanceScope,
        string? category,
        ReportFixContext? fixContext)
    {
        var familyScope = familyImpacts.Count switch
        {
            0 => "the affected family",
            1 => $"the {familyImpacts[0].FamilyName} family",
            _ => "the affected families"
        };

        if (fixContext?.FixEnabled == true
            && issueType == "MissingValue"
            && !string.IsNullOrWhiteSpace(parameterName)
            && fixContext.ParameterFillRules?.TryGetValue(parameterName, out var fillRule) == true)
        {
            return
            [
                "Use Apply Automatic Correction in the validation dialog.",
                $"{parameterName} will be filled from the {DescribeFillSource(fillRule)}.",
                "Re-run validation to confirm compliance."
            ];
        }

        if (fixContext?.FixEnabled == true
            && issueType == "NamingViolation"
            && !string.IsNullOrWhiteSpace(category)
            && fixContext.CategoryFixConfiguration?.TryGetValue(category, out var configuration) == true
            && configuration.PrefixFixScope != PrefixFixScope.None
            && !string.IsNullOrWhiteSpace(configuration.RequiredPrefix))
        {
            return
            [
                "Use Apply Automatic Correction in the validation dialog.",
                $"Affected {category} names will be updated to use the {configuration.RequiredPrefix} prefix.",
                "Re-run validation to confirm compliance."
            ];
        }

        if (instanceScope && issueType == "MissingValue")
        {
            return
            [
                $"Select the affected placed instance(s) in {familyScope}.",
                $"Enter a value for {parameterName} on each affected instance.",
                "Re-run validation."
            ];
        }

        return issueType switch
        {
            "MissingSharedParameter" or "MissingParameter" =>
            [
                $"Open {familyScope} in Revit.",
                $"Add the shared parameter {parameterName}.",
                "Load the family back into the project.",
                "Re-run validation."
            ],
            "InvalidSharedParameter" =>
            [
                $"Open {familyScope} in Revit.",
                $"Configure {parameterName} as a shared parameter using the company shared parameter file.",
                "Load the family back into the project.",
                "Re-run validation."
            ],
            "MissingValue" =>
            [
                $"Open {familyScope} in Revit.",
                $"Enter a value for {parameterName} on each affected family type.",
                "Load the family back into the project.",
                "Re-run validation."
            ],
            "ImportedCad" =>
            [
                $"Open {familyScope} in Revit.",
                "Remove imported CAD geometry or links from the family.",
                "Load the family back into the project.",
                "Re-run validation."
            ],
            "NamingViolation" =>
            [
                $"Open {familyScope} in Revit.",
                "Rename the affected family type(s) to match the required naming prefix.",
                "Load the family back into the project.",
                "Re-run validation."
            ],
            _ =>
            [
                $"Open {familyScope} in Revit.",
                "Update the family to meet the company standard.",
                "Load the family back into the project.",
                "Re-run validation."
            ]
        };
    }

    private static string BuildWhyFailed(
        string issueType,
        string? parameterName,
        EvidenceRecord sample,
        bool instanceScope)
    {
        if (instanceScope && issueType == "MissingValue")
        {
            return $"The parameter {parameterName} does not have the required value on the affected placed instance(s).";
        }

        return issueType switch
        {
            "MissingSharedParameter" =>
                $"The shared parameter {parameterName} is not present on the affected family types.",
            "MissingParameter" =>
                $"The parameter {parameterName} is not present on the affected family types.",
            "InvalidSharedParameter" =>
                $"The parameter {parameterName} is not configured correctly as a shared parameter on the affected family types.",
            "MissingValue" =>
                $"The parameter {parameterName} does not have the required value on the affected family types.",
            "ImportedCad" =>
                "Imported CAD geometry was found in one or more affected families.",
            "NamingViolation" =>
                "One or more family type names do not follow the required naming standard.",
            _ => NormalizeMessage(sample.Message)
        };
    }

    internal static IReadOnlyList<(string Label, string Value)> BuildProjectImpactLines(ProjectImpactSummary impact)
    {
        return
        [
            ("Placed Instances", impact.PlacedInstances.ToString()),
            ("Affected Instances", impact.AffectedInstances.ToString()),
            ("Affected Types", impact.AffectedTypes.ToString()),
            ("Affected Families", impact.AffectedFamilies.ToString())
        ];
    }

    internal static IReadOnlyList<(string Label, string Value)> BuildBusinessImpactLines(BusinessImpactSummary impact)
    {
        return
        [
            ("Families Requiring Correction", impact.FamiliesRequiringCorrection.ToString()),
            ("Types Affected", impact.TypesAffected.ToString()),
            ("Placed Instances Affected", impact.PlacedInstancesAffected.ToString())
        ];
    }

    internal static IReadOnlyList<(string Label, string Value)> BuildAutomaticCorrectionPreviewLines(
        AutomaticCorrectionPreview preview)
    {
        if (!preview.Available)
        {
            return [];
        }

        var lines = new List<(string Label, string Value)>
        {
            ("Action", preview.Action ?? string.Empty),
            ("Affected Types", preview.AffectedTypes.ToString()),
            ("Affected Instances", preview.AffectedInstances.ToString()),
            ("Expected Types Corrected", preview.ExpectedTypesCorrected.ToString()),
            ("Expected Instances Compliant", preview.ExpectedInstancesCompliant.ToString())
        };

        if (!string.IsNullOrWhiteSpace(preview.DefaultValue))
        {
            lines.Insert(1, ("Default Value", preview.DefaultValue));
        }

        return lines;
    }

    internal static Dictionary<string, string> BuildStructuredLines(
        string prefix,
        IReadOnlyList<(string Label, string Value)> lines)
    {
        var structuredData = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"{prefix}LineCount"] = lines.Count.ToString()
        };

        for (var index = 0; index < lines.Count; index++)
        {
            structuredData[$"{prefix}Line[{index}].label"] = lines[index].Label;
            structuredData[$"{prefix}Line[{index}].value"] = lines[index].Value;
        }

        return structuredData;
    }

    private static string FormatRecommendation(string issueTitle, IReadOnlyList<string> fixSteps)
    {
        if (fixSteps.Count == 0)
        {
            return issueTitle;
        }

        return $"{issueTitle}: {fixSteps[0]}";
    }

    private static string? TryGetStructuredValue(EvidenceRecord record, string key)
    {
        if (record.StructuredData is null)
        {
            return null;
        }

        return record.StructuredData.TryGetValue(key, out var value) ? value : null;
    }

    private static string NormalizeMessage(string message)
    {
        return message.Trim().TrimEnd('.');
    }

    private static bool ContainsAny(string message, params string[] values)
    {
        return values.Any(value => message.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
