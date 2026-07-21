using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Tests.Builders;

namespace BIMCapabilities.Engines.Report.Tests.Fixtures;

internal static class MultipleViolationsFixture
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
                        "Missing FireRating.",
                        "DR_001"),
                    ReportFixtureBuilder.CreateViolation(
                        "evidence-violation-002",
                        EvidenceSeverity.Critical,
                        "Invalid prefix.",
                        "DR_002"),
                    ReportFixtureBuilder.CreateViolation(
                        "evidence-violation-003",
                        EvidenceSeverity.Warning,
                        "Deprecated capability used.",
                        "DR_003")
                ]));
    }
}
