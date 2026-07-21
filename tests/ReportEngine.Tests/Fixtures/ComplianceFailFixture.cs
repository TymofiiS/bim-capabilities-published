using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Tests.Builders;

namespace BIMCapabilities.Engines.Report.Tests.Fixtures;

internal static class ComplianceFailFixture
{
    internal static ReportProfileRequest CreateRequest()
    {
        return ReportFixtureBuilder.CreateRequest(
            reportTitle: "Compliance Fail Report",
            evidence: ReportFixtureBuilder.CreateEvidenceCollection(
                records:
                [
                    ReportFixtureBuilder.CreateViolation(
                        "evidence-violation-001",
                        EvidenceSeverity.Error,
                        "Required shared parameter 'FireRating' is missing.",
                        "DR_HT_001"),
                    ReportFixtureBuilder.CreateViolation(
                        "evidence-violation-002",
                        EvidenceSeverity.Critical,
                        "Opening width exceeds maximum allowed value.",
                        "DR_HT_002")
                ]));
    }
}
