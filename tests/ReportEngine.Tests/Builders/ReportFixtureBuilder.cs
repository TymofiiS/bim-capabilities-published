using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Engines.Report.Tests.Builders;

/// <summary>
/// Builds deterministic evidence and diagnostics for report engine fixtures.
/// </summary>
internal static class ReportFixtureBuilder
{
    internal const string RuleId = "STD-ARC-OPENINGS-V01";
    internal const string DefaultCorrelationId = "corr-report-fixture-001";
    internal const string DefaultCollectionId = "evidence-collection-fixture-001";
    internal const string DefaultDiagnosticsCollectionId = "diagnostics-collection-fixture-001";

    internal static readonly DateTimeOffset BaseTimestamp = new(2026, 6, 19, 21, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset GeneratedAt = new(2026, 6, 19, 21, 30, 0, TimeSpan.Zero);

    internal static ReportProfileRequest CreateRequest(
        EvidenceCollection? evidence = null,
        DiagnosticCollection? diagnostics = null,
        string? reportTitle = null,
        string? correlationId = null)
    {
        return new ReportProfileRequest
        {
            RuleId = RuleId,
            ReportTitle = reportTitle ?? "Openings Compliance Report",
            Evidence = evidence,
            Diagnostics = diagnostics,
            CorrelationId = correlationId ?? DefaultCorrelationId,
            GeneratedAt = GeneratedAt
        };
    }

    internal static EvidenceRecord CreateViolation(
        string evidenceId,
        EvidenceSeverity severity,
        string message,
        string targetName,
        int sequenceOffsetMinutes = 0)
    {
        return new EvidenceRecord
        {
            EvidenceId = evidenceId,
            Timestamp = BaseTimestamp.AddMinutes(sequenceOffsetMinutes),
            Source = new EvidenceSource
            {
                EngineId = "parameter-engine",
                AtomId = "parameter.existence",
                RuleId = RuleId,
                CapabilityId = "parameter.existence"
            },
            Target = new EvidenceTarget
            {
                TargetType = "Element",
                TargetId = targetName,
                TargetName = targetName
            },
            Category = EvidenceCategory.Compliance,
            Severity = severity,
            Message = message
        };
    }

    internal static EvidenceRecord CreatePassingEvidence(string evidenceId, int sequenceOffsetMinutes = 0)
    {
        return new EvidenceRecord
        {
            EvidenceId = evidenceId,
            Timestamp = BaseTimestamp.AddMinutes(sequenceOffsetMinutes),
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

    internal static EvidenceCollection CreateEvidenceCollection(
        string? collectionId = null,
        string? correlationId = null,
        params EvidenceRecord[] records)
    {
        return new EvidenceCollection
        {
            CollectionId = collectionId ?? DefaultCollectionId,
            CorrelationId = correlationId ?? DefaultCorrelationId,
            Records = records
        };
    }

    internal static DiagnosticRecord CreateDiagnostic(
        string diagnosticId,
        string message = "Sample diagnostic for compliance report.",
        int sequenceOffsetMinutes = 0)
    {
        return new DiagnosticRecord
        {
            DiagnosticId = diagnosticId,
            Timestamp = BaseTimestamp.AddMinutes(sequenceOffsetMinutes),
            Source = new DiagnosticSource
            {
                ComponentType = "ReportEngine",
                ComponentId = "report-engine-tests",
                Code = "SampleDiagnostic"
            },
            Category = DiagnosticCategory.Runtime,
            Severity = DiagnosticSeverity.Information,
            Message = message,
            CorrelationId = DefaultCorrelationId
        };
    }

    internal static DiagnosticCollection CreateDiagnosticCollection(
        string? collectionId = null,
        string? correlationId = null,
        params DiagnosticRecord[] records)
    {
        return new DiagnosticCollection
        {
            CollectionId = collectionId ?? DefaultDiagnosticsCollectionId,
            CorrelationId = correlationId ?? DefaultCorrelationId,
            Records = records
        };
    }
}
