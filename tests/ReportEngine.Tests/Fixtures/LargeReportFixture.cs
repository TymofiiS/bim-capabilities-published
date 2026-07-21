using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Tests.Builders;

namespace BIMCapabilities.Engines.Report.Tests.Fixtures;

internal static class LargeReportFixture
{
    internal const int PassingRecordCount = 50;
    internal const int ViolationRecordCount = 50;

    internal static ReportProfileRequest CreateRequest()
    {
        var records = new List<EvidenceRecord>(PassingRecordCount + ViolationRecordCount);

        for (var index = 0; index < PassingRecordCount; index++)
        {
            records.Add(ReportFixtureBuilder.CreatePassingEvidence(
                $"evidence-pass-{index:D3}",
                sequenceOffsetMinutes: index));
        }

        for (var index = 0; index < ViolationRecordCount; index++)
        {
            records.Add(ReportFixtureBuilder.CreateViolation(
                $"evidence-violation-{index:D3}",
                (index % 3) switch
                {
                    0 => EvidenceSeverity.Error,
                    1 => EvidenceSeverity.Warning,
                    _ => EvidenceSeverity.Critical
                },
                $"Violation message {index:D3}.",
                $"DR_{index:D3}",
                sequenceOffsetMinutes: PassingRecordCount + index));
        }

        return ReportFixtureBuilder.CreateRequest(
            reportTitle: "Large Compliance Report",
            evidence: ReportFixtureBuilder.CreateEvidenceCollection(records: records.ToArray()),
            diagnostics: ReportFixtureBuilder.CreateDiagnosticCollection(
                records:
                [
                    ReportFixtureBuilder.CreateDiagnostic("diagnostic-large-001"),
                    ReportFixtureBuilder.CreateDiagnostic("diagnostic-large-002", "Large report diagnostic.")
                ]));
    }
}
