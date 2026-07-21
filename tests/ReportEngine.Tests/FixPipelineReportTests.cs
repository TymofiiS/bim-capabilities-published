using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;

namespace BIMCapabilities.Engines.Report.Tests;

public class FixPipelineReportTests
{
    [Fact]
    public void Correction_report_profile_renders_business_summary()
    {
        var profile = new CorrectionReportProfile();
        var output = profile.Prepare(new CorrectionReportRequest
        {
            RuleId = "DEMO-FIX-DOORS-V01",
            RuleName = "Demo Doors - Executable Fix",
            ParametersAdded = 11,
            ValuesAssigned = 11,
            NamesRenamed = 14,
            AffectedTypes = 11,
            DefaultValuesApplied = ["FireRating=EI60"],
            GeneratedAt = new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero)
        });

        var html = new HtmlReportRenderer().Render(output).Html;

        Assert.Contains("Correction Report", html);
        Assert.Contains("Parameters Added", html);
        Assert.Contains("Names Renamed", html);
        Assert.Contains("14", html);
        Assert.Contains("FireRating=EI60", html);
    }
}
