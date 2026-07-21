using BIMCapabilities.Contracts.Reports.Output;

namespace BIMCapabilities.Contracts.Tests;

internal static class ReportOutputTestData
{
    internal static ReportOutput CreateComplianceReportOutput()
    {
        return new ReportOutput
        {
            ReportId = "report-001",
            Title = "Openings Compliance Report",
            ProfileId = "compliance-report-v1",
            GeneratedAt = new DateTimeOffset(2026, 6, 19, 20, 0, 0, TimeSpan.Zero),
            Metadata = new ReportMetadata
            {
                RuleId = "STD-ARC-OPENINGS-V01",
                ProfileId = "compliance-report-v1",
                CorrelationId = "corr-001",
                GeneratedBy = "ReportEngine",
                Properties = new Dictionary<string, string>
                {
                    ["outputFormat"] = "unspecified"
                }
            },
            Sections =
            [
                CreateSection(
                    "Compliance Summary",
                    order: 1,
                    required: true,
                    CreateSummaryContent()),
                CreateSection(
                    "Violations",
                    order: 2,
                    required: true,
                    CreateViolationsContent())
            ]
        };
    }

    internal static ReportSection CreateSection(string name, int order, bool required, ReportContent? content = null)
    {
        return new ReportSection
        {
            Name = name,
            Description = $"{name} section.",
            Order = order,
            Required = required,
            Content = content
        };
    }

    private static ReportContent CreateSummaryContent()
    {
        return new ReportContent
        {
            Text = "Overall compliance status: Fail.",
            StructuredData = new Dictionary<string, string>
            {
                ["overallStatus"] = "Fail",
                ["totalViolations"] = "3"
            },
            EvidenceReferences =
            [
                new ReportReference
                {
                    ReferenceType = "Evidence",
                    ReferenceId = "evidence-001",
                    Description = "Parameter missing evidence."
                }
            ],
            DiagnosticReferences =
            [
                new ReportReference
                {
                    ReferenceType = "Diagnostic",
                    ReferenceId = "diagnostic-001",
                    Description = "Runtime diagnostic."
                }
            ]
        };
    }

    private static ReportContent CreateViolationsContent()
    {
        return new ReportContent
        {
            Text = "Three violations were detected.",
            EvidenceReferences =
            [
                new ReportReference
                {
                    ReferenceType = "Evidence",
                    ReferenceId = "evidence-002"
                }
            ],
            Attachments =
            [
                new ReportAttachment
                {
                    AttachmentId = "attachment-001",
                    ContentType = "text/plain",
                    FileName = "violations.txt",
                    Content = "Violation details."
                }
            ]
        };
    }
}
