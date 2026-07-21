using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Tests.Builders;

namespace BIMCapabilities.Engines.Report.Tests.Fixtures;

internal static class EmptyEvidenceFixture
{
    internal static ReportProfileRequest CreateRequest()
    {
        return ReportFixtureBuilder.CreateRequest(
            evidence: ReportFixtureBuilder.CreateEvidenceCollection());
    }
}
