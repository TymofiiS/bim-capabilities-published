using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Contracts.Reports.Rendering;
using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;
using BIMCapabilities.Engines.Report.Tests.Fixtures;
using BIMCapabilities.Engines.Report.Tests.Verification;

namespace BIMCapabilities.Engines.Report.Tests;

public class ReportEngineEndToEndTests
{
    private readonly ComplianceReportProfile _profile = new();
    private readonly HtmlReportRenderer _htmlRenderer = new();
    private readonly JsonReportRenderer _jsonRenderer = new();

    [Fact]
    public void Empty_report_end_to_end_pipeline_renders_html_and_json()
    {
        var (output, html, json) = RunPipeline(EmptyEvidenceFixture.CreateRequest());

        AssertSectionExists(output, "Compliance Summary");
        AssertSectionExists(output, "Grouped Findings");
        AssertSectionExists(output, "Recommendations");
        AssertSectionExists(output, "Evidence");
        AssertSectionExists(output, "Diagnostics");

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("Pass", summary.StructuredData!["resultStatus"]);
        Assert.Equal("100", summary.StructuredData!["compliancePercentage"]);

        Assert.Contains("No compliance issues were detected.", html.Html);
        Assert.Contains("\"compliancePercentage\": \"100\"", json.Json);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Fact]
    public void Compliance_pass_end_to_end_pipeline_renders_html_and_json()
    {
        var (output, html, json) = RunPipeline(CompliancePassFixture.CreateRequest());

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("Pass", summary.StructuredData!["resultStatus"]);
        Assert.Equal("100", summary.StructuredData!["compliancePercentage"]);

        var findings = GetSection(output, "Grouped Findings").Content!;
        Assert.Equal("0", findings.StructuredData!["issueGroupCount"]);

        Assert.Contains("<h1>Compliance Pass Report</h1>", html.Html);
        Assert.Contains("\"title\": \"Compliance Pass Report\"", json.Json);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Fact]
    public void Compliance_fail_end_to_end_pipeline_renders_html_and_json()
    {
        var (output, html, json) = RunPipeline(ComplianceFailFixture.CreateRequest());

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("Fail", summary.StructuredData!["resultStatus"]);
        Assert.Equal("2", summary.StructuredData!["failedObjects"]);
        Assert.Equal("2", summary.StructuredData!["issuesFound"]);

        var findings = GetSection(output, "Grouped Findings").Content!;
        Assert.Equal("2", findings.StructuredData!["issueGroupCount"]);
        Assert.Contains("DR_HT_001", html.Html);
        Assert.Contains("DR_HT_002", html.Html);
        Assert.Contains("Opening width exceeds maximum allowed value.", json.Json);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Fact]
    public void Single_violation_end_to_end_pipeline_renders_html_and_json()
    {
        var (output, html, json) = RunPipeline(SingleViolationFixture.CreateRequest());

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("1", summary.StructuredData!["issuesFound"]);
        Assert.Equal("Fail", summary.StructuredData!["resultStatus"]);

        var findings = GetSection(output, "Grouped Findings").Content!;
        Assert.Equal("1", findings.StructuredData!["issueGroupCount"]);

        var evidence = GetSection(output, "Evidence").Content!;
        Assert.Equal("1", evidence.StructuredData!["totalEvidence"]);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Fact]
    public void Multiple_violations_end_to_end_pipeline_renders_html_and_json()
    {
        var (output, html, json) = RunPipeline(MultipleViolationsFixture.CreateRequest());

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("3", summary.StructuredData!["issuesFound"]);
        Assert.Equal("Fail", summary.StructuredData!["resultStatus"]);

        var findings = GetSection(output, "Grouped Findings").Content!;
        Assert.Equal("3", findings.StructuredData!["issueGroupCount"]);
        Assert.Contains("Missing FireRating.", json.Json);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Fact]
    public void Mixed_severities_end_to_end_pipeline_renders_html_and_json()
    {
        var (output, html, json) = RunPipeline(MixedSeverityFixture.CreateRequest());

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("2", summary.StructuredData!["issuesFound"]);

        var evidence = GetSection(output, "Evidence").Content!;
        Assert.Equal("4", evidence.StructuredData!["totalEvidence"]);

        var diagnostics = GetSection(output, "Diagnostics").Content!;
        Assert.Equal("2", diagnostics.StructuredData!["totalDiagnostics"]);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Fact]
    public void Large_report_end_to_end_pipeline_renders_html_and_json()
    {
        var (output, html, json) = RunPipeline(LargeReportFixture.CreateRequest());

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("50", summary.StructuredData!["issuesFound"]);

        var evidence = GetSection(output, "Evidence").Content!;
        Assert.Equal("100", evidence.StructuredData!["totalEvidence"]);

        var findings = GetSection(output, "Grouped Findings").Content!;
        Assert.Equal("50", findings.StructuredData!["issueGroupCount"]);

        var diagnostics = GetSection(output, "Diagnostics").Content!;
        Assert.Equal("2", diagnostics.StructuredData!["totalDiagnostics"]);

        Assert.Contains("evidence-violation-049", json.Json);
        Assert.Contains("diagnostic-large-001", json.Json);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Fact]
    public void End_to_end_pipeline_supports_attachment_rendering()
    {
        var output = _profile.Prepare(CompliancePassFixture.CreateRequest());
        output = output with
        {
            Sections = output.Sections
                .Select(section => section.Name == "Evidence"
                    ? section with
                    {
                        Content = section.Content! with
                        {
                            Attachments =
                            [
                                new ReportAttachment
                                {
                                    AttachmentId = "attachment-e2e-001",
                                    ContentType = "application/json",
                                    FileName = "summary.json",
                                    Content = """{"status":"pass"}"""
                                }
                            ]
                        }
                    }
                    : section)
                .ToArray()
        };

        var html = _htmlRenderer.Render(output);
        var json = _jsonRenderer.Render(output);

        Assert.Contains("\"attachmentId\": \"attachment-e2e-001\"", json.Json);
        Assert.Contains("\"fileName\": \"summary.json\"", json.Json);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Fact]
    public void End_to_end_pipeline_supports_diagnostic_rendering()
    {
        var (output, html, json) = RunPipeline(MixedSeverityFixture.CreateRequest());

        Assert.DoesNotContain("Diagnostic References", html.Html);
        Assert.Contains("\"referenceType\": \"Diagnostic\"", json.Json);
        Assert.Contains("\"referenceId\": \"diagnostic-002\"", json.Json);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);
    }

    [Theory]
    [MemberData(nameof(FixtureRequests))]
    public void Renderer_consistency_is_preserved_for_all_fixtures(string fixtureName, ReportProfileRequest request)
    {
        var (output, html, json) = RunPipeline(request);

        ReportRendererConsistencyVerifier.AssertConsistent(output, html.Html, json.Json);

        var roundTrip = JsonSerializer.Deserialize<ReportOutput>(json.Json, ReportOutputJsonSerialization.Options);
        Assert.NotNull(roundTrip);
        Assert.Equal(output.ReportId, roundTrip.ReportId);
        Assert.Equal(output.Title, roundTrip.Title);
        Assert.Equal(output.Sections.Count, roundTrip.Sections.Count);
        Assert.False(string.IsNullOrWhiteSpace(fixtureName));
    }

    [Fact]
    public void End_to_end_html_rendering_produces_self_contained_documents()
    {
        var fixtures = new Func<ReportProfileRequest>[]
        {
            EmptyEvidenceFixture.CreateRequest,
            CompliancePassFixture.CreateRequest,
            ComplianceFailFixture.CreateRequest,
            LargeReportFixture.CreateRequest
        };

        foreach (var createRequest in fixtures)
        {
            var html = _htmlRenderer.Render(_profile.Prepare(createRequest()));
            ReportRendererConsistencyVerifier.AssertValidHtml(html.Html);
            Assert.Equal("text/html; charset=utf-8", html.ContentType);
            Assert.Equal(html.Html, html.FileContent);
        }
    }

    [Fact]
    public void End_to_end_json_rendering_produces_valid_deterministic_documents()
    {
        var fixtures = new Func<ReportProfileRequest>[]
        {
            EmptyEvidenceFixture.CreateRequest,
            SingleViolationFixture.CreateRequest,
            MixedSeverityFixture.CreateRequest,
            LargeReportFixture.CreateRequest
        };

        foreach (var createRequest in fixtures)
        {
            var output = _profile.Prepare(createRequest());
            var first = _jsonRenderer.Render(output);
            var second = _jsonRenderer.Render(output);

            ReportRendererConsistencyVerifier.AssertValidJson(first.Json);
            Assert.Equal(first.Json, second.Json);
            Assert.Equal("application/json; charset=utf-8", first.ContentType);
            Assert.Equal(first.Json, first.DocumentContent);
        }
    }

    public static TheoryData<string, ReportProfileRequest> FixtureRequests =>
        new()
        {
            { nameof(EmptyEvidenceFixture), EmptyEvidenceFixture.CreateRequest() },
            { nameof(SingleViolationFixture), SingleViolationFixture.CreateRequest() },
            { nameof(MultipleViolationsFixture), MultipleViolationsFixture.CreateRequest() },
            { nameof(MixedSeverityFixture), MixedSeverityFixture.CreateRequest() },
            { nameof(CompliancePassFixture), CompliancePassFixture.CreateRequest() },
            { nameof(ComplianceFailFixture), ComplianceFailFixture.CreateRequest() },
            { nameof(LargeReportFixture), LargeReportFixture.CreateRequest() }
        };

    private (ReportOutput Output, HtmlRenderResult Html, JsonRenderResult Json) RunPipeline(ReportProfileRequest request)
    {
        var output = _profile.Prepare(request);
        var html = _htmlRenderer.Render(output);
        var json = _jsonRenderer.Render(output);
        return (output, html, json);
    }

    private static void AssertSectionExists(ReportOutput output, string sectionName)
    {
        Assert.Contains(output.Sections, section => section.Name == sectionName);
    }

    private static ReportSection GetSection(ReportOutput output, string sectionName)
    {
        return output.Sections.First(section => section.Name == sectionName);
    }
}
