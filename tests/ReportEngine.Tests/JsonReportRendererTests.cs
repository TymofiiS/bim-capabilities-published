using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Rendering;
using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;

namespace BIMCapabilities.Engines.Report.Tests;

public class JsonReportRendererTests
{
    private readonly JsonReportRenderer _renderer = new();
    private readonly ComplianceReportProfile _complianceProfile = new();

    [Fact]
    public void Json_report_renderer_implements_required_interface()
    {
        Assert.IsAssignableFrom<IJsonReportRenderer>(_renderer);
        Assert.Equal("json", _renderer.Format);
    }

    [Fact]
    public void Render_produces_valid_json_for_empty_report()
    {
        var result = _renderer.Render(HtmlReportRendererTestData.CreateEmptyReport());

        AssertValidJson(result.Json);
        Assert.Equal(result.Json, result.DocumentContent);
        Assert.Equal("application/json; charset=utf-8", result.ContentType);
        Assert.Contains("\"title\": \"Empty Report\"", result.Json);
        Assert.Contains("\"sections\": []", result.Json);
    }

    [Fact]
    public void Render_produces_simple_report_json()
    {
        var result = _renderer.Render(HtmlReportRendererTestData.CreateSimpleReport());

        AssertValidJson(result.Json);
        Assert.Contains("\"ruleId\": \"STD-ARC-OPENINGS-V01\"", result.Json);
        Assert.Contains("\"name\": \"Summary\"", result.Json);
        Assert.Contains("All checks passed.", result.Json);
    }

    [Fact]
    public void Render_produces_compliance_report_json()
    {
        var reportOutput = CreateComplianceReportOutput();
        var result = _renderer.Render(reportOutput);

        AssertValidJson(result.Json);
        Assert.Contains("\"title\": \"Openings Compliance Report\"", result.Json);
        Assert.Contains("\"name\": \"Compliance Summary\"", result.Json);
        Assert.Contains("\"name\": \"Grouped Findings\"", result.Json);
        Assert.Contains("\"name\": \"Evidence\"", result.Json);
        Assert.Contains("\"name\": \"Diagnostics\"", result.Json);
        Assert.Contains("compliancePercentage", result.Json);
        Assert.Contains("Required parameter missing.", result.Json);
    }

    [Fact]
    public void Render_orders_multiple_sections_deterministically()
    {
        var result = _renderer.Render(HtmlReportRendererTestData.CreateMultiSectionReport());

        var sectionAIndex = result.Json.IndexOf("\"name\": \"Section A\"", StringComparison.Ordinal);
        var sectionBIndex = result.Json.IndexOf("\"name\": \"Section B\"", StringComparison.Ordinal);

        Assert.True(sectionAIndex >= 0);
        Assert.True(sectionBIndex > sectionAIndex);
    }

    [Fact]
    public void Render_includes_evidence_references()
    {
        var reportOutput = _complianceProfile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "evidence-violation-001",
                    EvidenceSeverity.Error,
                    "Missing FireRating.",
                    "DR_001"))));

        var result = _renderer.Render(reportOutput);

        Assert.Contains("\"referenceType\": \"Evidence\"", result.Json);
        Assert.Contains("\"referenceId\": \"evidence-violation-001\"", result.Json);
        Assert.Contains("Missing FireRating.", result.Json);
    }

    [Fact]
    public void Render_includes_diagnostic_references()
    {
        var reportOutput = _complianceProfile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(),
            ComplianceReportProfileTestData.CreateDiagnosticCollection(
                ComplianceReportProfileTestData.CreateDiagnostic("diagnostic-001"))));

        var result = _renderer.Render(reportOutput);

        Assert.Contains("\"referenceType\": \"Diagnostic\"", result.Json);
        Assert.Contains("\"referenceId\": \"diagnostic-001\"", result.Json);
    }

    [Fact]
    public void Render_includes_attachments()
    {
        var report = HtmlReportRendererTestData.CreateSimpleReport() with
        {
            Sections =
            [
                new ReportSection
                {
                    Name = "Attachments Section",
                    Order = 1,
                    Required = true,
                    Content = new ReportContent
                    {
                        Text = "Report with attachment.",
                        Attachments =
                        [
                            new ReportAttachment
                            {
                                AttachmentId = "attachment-001",
                                ContentType = "application/json",
                                FileName = "details.json",
                                Content = """{"detail":"value"}"""
                            }
                        ]
                    }
                }
            ]
        };

        var result = _renderer.Render(report);

        Assert.Contains("\"attachmentId\": \"attachment-001\"", result.Json);
        Assert.Contains("\"fileName\": \"details.json\"", result.Json);
        Assert.Contains("\"content\": \"{\\\"detail\\\":\\\"value\\\"}\"", result.Json);
    }

    [Fact]
    public void Render_produces_deterministic_json_output()
    {
        var reportOutput = CreateComplianceReportOutput();
        var first = _renderer.Render(reportOutput).Json;
        var second = _renderer.Render(reportOutput).Json;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Render_preserves_complete_report_output_structure()
    {
        var original = CreateComplianceReportOutput();
        var result = _renderer.Render(original);
        var roundTrip = JsonSerializer.Deserialize<ReportOutput>(result.Json, ReportOutputJsonSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.ReportId, roundTrip.ReportId);
        Assert.Equal(original.Title, roundTrip.Title);
        Assert.Equal(original.ProfileId, roundTrip.ProfileId);
        Assert.Equal(original.Sections.Count, roundTrip.Sections.Count);
        Assert.Equal(original.Metadata!.RuleId, roundTrip.Metadata!.RuleId);
    }

    [Fact]
    public void Json_renderer_does_not_write_files()
    {
        var methods = typeof(JsonReportRenderer).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.All(methods, method =>
        {
            Assert.DoesNotContain("Save", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Write", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Launch", method.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static ReportOutput CreateComplianceReportOutput()
    {
        var profile = new ComplianceReportProfile();
        return profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "evidence-violation-001",
                    EvidenceSeverity.Error,
                    "Required parameter missing.",
                    "DR_HT_001"),
                ComplianceReportProfileTestData.CreatePassingEvidence("evidence-pass-001")),
            ComplianceReportProfileTestData.CreateDiagnosticCollection(
                ComplianceReportProfileTestData.CreateDiagnostic("diagnostic-001"))));
    }

    private static void AssertValidJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }
}
