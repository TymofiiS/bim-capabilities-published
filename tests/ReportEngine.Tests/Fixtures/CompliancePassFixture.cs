using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Tests.Builders;

namespace BIMCapabilities.Engines.Report.Tests.Fixtures;

internal static class CompliancePassFixture
{
    internal static ReportProfileRequest CreateRequest()
    {
        return ReportFixtureBuilder.CreateRequest(
            reportTitle: "Compliance Pass Report",
            evidence: ReportFixtureBuilder.CreateEvidenceCollection(
                records:
                [
                    ReportFixtureBuilder.CreatePassingEvidence("evidence-pass-001"),
                    ReportFixtureBuilder.CreatePassingEvidence("evidence-pass-002"),
                    ReportFixtureBuilder.CreatePassingEvidence("evidence-pass-003")
                ]));
    }
}
