using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;

namespace BIMCapabilities.Engines.Report.Tests;

public class FindingFixGuidanceReportTests
{
    private readonly ComplianceReportProfile _profile = new();
    private readonly HtmlReportRenderer _htmlRenderer = new();

    [Fact]
    public void Prepare_includes_why_and_fix_steps_for_missing_shared_parameter()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "shared-parameter-missing-dr-001-my_firerating",
                    EvidenceSeverity.Error,
                    "Required shared parameter 'MY_FireRating' is missing on 'DR_001'.",
                    "DR_001",
                    "MY_FireRating"))));

        var findings = output.Sections.First(section => section.Name == "Grouped Findings").Content!;
        Assert.Equal("The shared parameter MY_FireRating is not present on the affected family types.", findings.StructuredData!["group[0].whyFailed"]);
        Assert.Equal("Open the DR_001 family in Revit.", findings.StructuredData!["group[0].fixStep[0]"]);
        Assert.Equal("Re-run validation.", findings.StructuredData!["group[0].fixStep[3]"]);
    }

    [Fact]
    public void Html_renderer_shows_why_and_numbered_fix_steps_for_each_finding()
    {
        var report = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-missing-dr-001-my_firerating",
                    EvidenceSeverity.Error,
                    "Required parameter 'MY_FireRating' is missing.",
                    "DR_001",
                    "MY_FireRating"))));

        var html = _htmlRenderer.Render(report).Html;

        Assert.Contains("<h4>Why</h4>", html);
        Assert.Contains("<h4>How to fix</h4>", html);
        Assert.Contains("<ol class=\"fix-steps\">", html);
        Assert.Contains("Load the family back into the project.", html);
    }
}
