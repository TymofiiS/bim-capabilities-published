using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Tests.Builders;

namespace BIMCapabilities.Engines.Report.Tests.Fixtures;

internal static class SingleViolationFixture
{
    internal static ReportProfileRequest CreateRequest()
    {
        return ReportFixtureBuilder.CreateRequest(
            evidence: ReportFixtureBuilder.CreateEvidenceCollection(
                records:
                [
                    ReportFixtureBuilder.CreateViolation(
                        "evidence-violation-001",
                        EvidenceSeverity.Error,
                        "Required parameter missing.",
                        "DR_HT_001")
                ]));
    }
}
