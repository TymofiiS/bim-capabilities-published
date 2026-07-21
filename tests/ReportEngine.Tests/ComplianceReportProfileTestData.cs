using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Engines.Report.Tests;

internal static class ComplianceReportProfileTestData
{
    internal const string RuleId = "STD-ARC-OPENINGS-V01";

    internal static ReportProfileRequest CreateRequest(
        EvidenceCollection? evidence = null,
        DiagnosticCollection? diagnostics = null,
        bool fixEnabled = false,
        IReadOnlyDictionary<string, string>? parameterDefaults = null,
        IReadOnlyDictionary<string, string>? parameterFillRules = null,
        IReadOnlyDictionary<string, ReportCategoryFixConfiguration>? categoryFixConfiguration = null,
        string? ruleName = null)
    {
        return new ReportProfileRequest
        {
            RuleId = RuleId,
            RuleName = ruleName,
            ReportTitle = "Openings Compliance Report",
            Evidence = evidence,
            Diagnostics = diagnostics,
            CorrelationId = "corr-compliance-001",
            GeneratedAt = new DateTimeOffset(2026, 6, 19, 21, 0, 0, TimeSpan.Zero),
            FixEnabled = fixEnabled,
            ParameterDefaults = parameterDefaults,
            ParameterFillRules = parameterFillRules,
            CategoryFixConfiguration = categoryFixConfiguration
        };
    }

    internal static EvidenceRecord CreateViolation(
        string evidenceId,
        EvidenceSeverity severity,
        string message,
        string targetName,
        string? parameterName = null,
        string? familyName = null,
        string? categoryName = null,
        int placedInstanceCount = 0)
    {
        var structuredData = parameterName is null
            ? null
            : new Dictionary<string, string>
            {
                ["parameterName"] = parameterName,
                ["typeName"] = targetName,
                ["placedInstanceCount"] = placedInstanceCount.ToString()
            };

        if (structuredData is not null)
        {
            if (!string.IsNullOrWhiteSpace(familyName))
            {
                structuredData["familyName"] = familyName;
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                structuredData["categoryName"] = categoryName;
            }
        }

        return new EvidenceRecord
        {
            EvidenceId = evidenceId,
            Timestamp = new DateTimeOffset(2026, 6, 19, 21, 0, 0, TimeSpan.Zero),
            Source = new EvidenceSource
            {
                EngineId = "parameter-engine",
                AtomId = "parameter.existence",
                RuleId = RuleId,
                CapabilityId = "parameter.existence"
            },
            Target = new EvidenceTarget
            {
                TargetType = "familyType",
                TargetId = targetName,
                TargetName = targetName,
                TargetSetDescription = categoryName ?? "Doors"
            },
            Category = EvidenceCategory.Compliance,
            Severity = severity,
            Message = message,
            StructuredData = structuredData
        };
    }

    internal static EvidenceRecord CreateInstanceValueViolation(
        string evidenceId,
        string instanceId,
        string instanceName,
        string familyName,
        string typeName,
        string parameterName,
        string categoryName = "Windows")
    {
        return new EvidenceRecord
        {
            EvidenceId = evidenceId,
            Timestamp = new DateTimeOffset(2026, 6, 19, 21, 0, 0, TimeSpan.Zero),
            Source = new EvidenceSource
            {
                EngineId = "parameter-engine",
                AtomId = "parameter.validation.value",
                RuleId = RuleId,
                CapabilityId = "parameter.validation.value"
            },
            Target = new EvidenceTarget
            {
                TargetType = "familyInstance",
                TargetId = instanceId,
                TargetName = instanceName,
                TargetSetDescription = categoryName
            },
            Category = EvidenceCategory.Compliance,
            Severity = EvidenceSeverity.Error,
            Message = $"Parameter '{parameterName}' on instance '{instanceName}' is missing a required value.",
            StructuredData = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["objectKind"] = "familyInstance",
                ["validationScope"] = "instance",
                ["parameterName"] = parameterName,
                ["familyName"] = familyName,
                ["typeName"] = typeName,
                ["categoryName"] = categoryName
            }
        };
    }

    internal static EvidenceRecord CreatePassingEvidence(string evidenceId)
    {
        return new EvidenceRecord
        {
            EvidenceId = evidenceId,
            Timestamp = new DateTimeOffset(2026, 6, 19, 21, 1, 0, TimeSpan.Zero),
            Source = new EvidenceSource
            {
                EngineId = "naming-engine",
                AtomId = "naming.prefix.validation",
                RuleId = RuleId
            },
            Category = EvidenceCategory.Compliance,
            Severity = EvidenceSeverity.Info,
            Message = "Naming prefix validation passed."
        };
    }

    internal static EvidenceCollection CreateEvidenceCollection(params EvidenceRecord[] records)
    {
        return new EvidenceCollection
        {
            CollectionId = "evidence-collection-001",
            CorrelationId = "corr-compliance-001",
            Records = records
        };
    }

    internal static DiagnosticCollection CreateDiagnosticCollection(params DiagnosticRecord[] records)
    {
        return new DiagnosticCollection
        {
            CollectionId = "diagnostics-collection-001",
            CorrelationId = "corr-compliance-001",
            Records = records
        };
    }

    internal static DiagnosticRecord CreateDiagnostic(string diagnosticId)
    {
        return new DiagnosticRecord
        {
            DiagnosticId = diagnosticId,
            Timestamp = new DateTimeOffset(2026, 6, 19, 21, 2, 0, TimeSpan.Zero),
            Source = new DiagnosticSource
            {
                ComponentType = "Runtime",
                ComponentId = "runtime-skeleton",
                Code = "SampleDiagnostic"
            },
            Category = DiagnosticCategory.Runtime,
            Severity = DiagnosticSeverity.Information,
            Message = "Sample diagnostic for compliance report.",
            CorrelationId = "corr-compliance-001"
        };
    }
}
