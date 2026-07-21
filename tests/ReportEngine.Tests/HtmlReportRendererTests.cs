using System.Reflection;
using System.Text.RegularExpressions;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Rendering;
using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;

namespace BIMCapabilities.Engines.Report.Tests;

public partial class HtmlReportRendererTests
{
    private readonly HtmlReportRenderer _renderer = new();
    private readonly ComplianceReportProfile _complianceProfile = new();

    [Fact]
    public void Html_report_renderer_implements_required_interfaces()
    {
        Assert.IsAssignableFrom<IReportRenderer>(_renderer);
        Assert.IsAssignableFrom<IHtmlReportRenderer>(_renderer);
        Assert.Equal("html", _renderer.Format);
    }

    [Fact]
    public void Render_produces_valid_html5_for_empty_report()
    {
        var result = _renderer.Render(HtmlReportRendererTestData.CreateEmptyReport());

        AssertValidHtmlDocument(result.Html);
        Assert.Contains("<h1>Empty Report</h1>", result.Html);
        Assert.Equal(result.Html, result.FileContent);
        Assert.Equal("text/html; charset=utf-8", result.ContentType);
        Assert.DoesNotContain("<script", result.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_produces_simple_report_html()
    {
        var report = _complianceProfile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection()));

        var result = _renderer.Render(report);

        Assert.Contains("<h1>Openings Compliance Report</h1>", result.Html);
        Assert.Contains("<h2>Result Summary</h2>", result.Html);
        Assert.Contains("Pass", result.Html);
        Assert.Contains("Openings Compliance Report", result.Html);
    }

    [Fact]
    public void Render_orders_coordinator_sections_in_expected_layout()
    {
        var report = _complianceProfile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "evidence-violation-001",
                    EvidenceSeverity.Error,
                    "Required parameter missing.",
                    "DR_HT_001",
                    "MY_FireRating"))));

        var result = _renderer.Render(report);

        var ruleInformationPosition = result.Html.IndexOf("Rule Information", StringComparison.Ordinal);
        var resultSummaryPosition = result.Html.IndexOf("Result Summary", StringComparison.Ordinal);
        var projectImpactPosition = result.Html.IndexOf("Project Impact", StringComparison.Ordinal);
        var businessImpactPosition = result.Html.IndexOf("Business Impact", StringComparison.Ordinal);
        var groupedFindingsPosition = result.Html.IndexOf("Grouped Findings", StringComparison.Ordinal);
        var recommendationsPosition = result.Html.IndexOf("Recommendations", StringComparison.Ordinal);

        Assert.True(ruleInformationPosition >= 0);
        Assert.True(resultSummaryPosition > ruleInformationPosition);
        Assert.True(projectImpactPosition > resultSummaryPosition);
        Assert.True(businessImpactPosition > projectImpactPosition);
        Assert.True(groupedFindingsPosition > businessImpactPosition);
        Assert.True(recommendationsPosition > groupedFindingsPosition);
    }

    [Fact]
    public void Render_produces_coordinator_compliance_report_html()
    {
        var reportOutput = _complianceProfile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "evidence-violation-001",
                    EvidenceSeverity.Error,
                    "Required parameter missing.",
                    "DR_HT_001",
                    "MY_FireRating"),
                ComplianceReportProfileTestData.CreatePassingEvidence("evidence-pass-001")),
            ComplianceReportProfileTestData.CreateDiagnosticCollection(
                ComplianceReportProfileTestData.CreateDiagnostic("diagnostic-001"))));

        var result = _renderer.Render(reportOutput);

        Assert.Contains("<h1>Openings Compliance Report</h1>", result.Html);
        Assert.Contains("<h2>Rule Information</h2>", result.Html);
        Assert.Contains("<h2>Validation Scope</h2>", result.Html);
        Assert.Contains("<h2>Result Summary</h2>", result.Html);
        Assert.Contains("<h2>Grouped Findings</h2>", result.Html);
        Assert.Contains("<h2>Recommendations</h2>", result.Html);
        Assert.Contains("Missing MY_FireRating", result.Html);
        Assert.Contains("<h4>Why</h4>", result.Html);
        Assert.DoesNotContain("target-set-", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<h4>How to fix</h4>", result.Html);
        Assert.Contains("Add the shared parameter MY_FireRating", result.Html);
        Assert.DoesNotContain("Diagnostics", result.Html);
        Assert.DoesNotContain("correlation", result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Evidence References", result.Html);
    }

    [Fact]
    public void Render_orders_multiple_sections_by_order_value()
    {
        var report = _complianceProfile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreatePassingEvidence("evidence-pass-001"))));

        var result = _renderer.Render(report);

        var summaryPosition = result.Html.IndexOf("Result Summary", StringComparison.Ordinal);
        var findingsPosition = result.Html.IndexOf("Grouped Findings", StringComparison.Ordinal);

        Assert.True(summaryPosition >= 0);
        Assert.True(findingsPosition > summaryPosition);
    }

    [Fact]
    public void Render_encodes_html_special_characters()
    {
        var report = HtmlReportRendererTestData.CreateSimpleReport() with
        {
            Title = "Report <Test> & \"Quotes\""
        };

        var result = _renderer.Render(report);

        Assert.Contains("Report &lt;Test&gt; &amp; &quot;Quotes&quot;", result.Html);
        Assert.DoesNotContain("Report <Test>", result.Html);
    }

    [Fact]
    public void Html_renderer_does_not_write_files_or_launch_browser()
    {
        var rendererType = typeof(HtmlReportRenderer);
        var methods = rendererType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.All(methods, method =>
        {
            Assert.DoesNotContain("Launch", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Save", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Write", method.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static void AssertValidHtmlDocument(string html)
    {
        Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<head>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<body>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<main", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("</html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(HtmlScriptPattern(), html);
    }

    [GeneratedRegex("<script", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlScriptPattern();
}
