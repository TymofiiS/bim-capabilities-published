using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Tests.Builders;

namespace BIMCapabilities.Engines.Report.Tests.Fixtures;

internal static class MixedSeverityFixture
{
    internal static ReportProfileRequest CreateRequest()
    {
        return ReportFixtureBuilder.CreateRequest(
            evidence: ReportFixtureBuilder.CreateEvidenceCollection(
                records:
                [
                    ReportFixtureBuilder.CreatePassingEvidence("evidence-pass-001"),
                    ReportFixtureBuilder.CreatePassingEvidence("evidence-pass-002"),
                    ReportFixtureBuilder.CreateViolation(
                        "evidence-violation-001",
                        EvidenceSeverity.Warning,
                        "Deprecated capability used.",
                        "DR_003"),
                    ReportFixtureBuilder.CreateViolation(
                        "evidence-violation-002",
                        EvidenceSeverity.Error,
                        "Missing parameter.",
                        "DR_004")
                ]),
            diagnostics: ReportFixtureBuilder.CreateDiagnosticCollection(
                records:
                [
                    ReportFixtureBuilder.CreateDiagnostic("diagnostic-001"),
                    ReportFixtureBuilder.CreateDiagnostic("diagnostic-002", "Secondary diagnostic message.")
                ]));
    }
}
