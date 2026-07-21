using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;

namespace BIMCapabilities.Engines.Report.Tests;

public class BusinessImpactReportTests
{
    private readonly ComplianceReportProfile _profile = new();
    private readonly HtmlReportRenderer _htmlRenderer = new();

    [Fact]
    public void Prepare_includes_project_and_business_impact_sections()
    {
        var output = _profile.Prepare(CreateRequestWithDoorFailure());

        AssertSectionExists(output, "Project Impact");
        AssertSectionExists(output, "Business Impact");
        AssertSectionExists(output, "Root Cause");
        AssertSectionExists(output, "Automatic Correction Preview");

        var projectImpact = GetSection(output, "Project Impact").Content!;
        Assert.Equal("2", projectImpact.StructuredData!["projectImpactLine[0].value"]);
        Assert.Equal("2", projectImpact.StructuredData!["projectImpactLine[1].value"]);
        Assert.Equal("2", projectImpact.StructuredData!["projectImpactLine[2].value"]);
        Assert.Equal("1", projectImpact.StructuredData!["projectImpactLine[3].value"]);

        var businessImpact = GetSection(output, "Business Impact").Content!;
        Assert.Equal("1", businessImpact.StructuredData!["businessImpactLine[0].value"]);
        Assert.Equal("2", businessImpact.StructuredData!["businessImpactLine[1].value"]);
        Assert.Equal("2", businessImpact.StructuredData!["businessImpactLine[2].value"]);

        var preview = GetSection(output, "Automatic Correction Preview").Content!;
        Assert.Equal("True", preview.StructuredData!["available"]);
        Assert.Equal("MY_FireRating=EI60", preview.StructuredData!["correctionPreviewLine[1].value"]);
        Assert.Contains("Create MY_FireRating with default EI60", preview.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Prepare_root_cause_summary_covers_all_issue_groups()
    {
        var output = _profile.Prepare(CreateRequestWithDoorAndWindowFailures());
        var rootCause = GetSection(output, "Root Cause").Content!;

        Assert.Contains("MY_FireRating", rootCause.Text, StringComparison.Ordinal);
        Assert.Contains("MY_Room", rootCause.Text, StringComparison.Ordinal);
        Assert.Contains("2 required parameters", rootCause.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Project Impact", rootCause.Text, StringComparison.Ordinal);
    }

    private static ReportProfileRequest CreateRequestWithDoorAndWindowFailures()
    {
        return ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-missing-door-001",
                    EvidenceSeverity.Error,
                    "Required parameter 'MY_FireRating' is missing on '900x2100'.",
                    "900x2100",
                    "MY_FireRating",
                    "Single Flush",
                    "Doors",
                    1),
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-missing-window-001",
                    EvidenceSeverity.Error,
                    "Required parameter 'MY_Room' is missing on '1200 x 1500mm'.",
                    "1200 x 1500mm",
                    "MY_Room",
                    "Casement",
                    "Windows",
                    2)),
            ComplianceReportProfileTestData.CreateDiagnosticCollection(
                CreateScopeDiagnostic("Doors", placedInstances: 1, typesChecked: 2, familiesChecked: 1),
                CreateScopeDiagnostic("Windows", placedInstances: 2, typesChecked: 1, familiesChecked: 1)));
    }

    [Fact]
    public void Html_renderer_hides_internal_identifiers_and_shows_family_groups()
    {
        var report = _profile.Prepare(CreateRequestWithDoorFailure());
        var html = _htmlRenderer.Render(report).Html;

        Assert.Contains("Project Impact", html);
        Assert.Contains("Business Impact", html);
        Assert.Contains("Automatic Correction Available", html);
        Assert.Contains("Family: Single Flush", html);
        Assert.Contains("900x2100", html);
        Assert.DoesNotContain("target-set-", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("corr-", html, StringComparison.OrdinalIgnoreCase);
    }

    private static ReportProfileRequest CreateRequestWithDoorFailure()
    {
        return ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-missing-type-001-my_firerating",
                    EvidenceSeverity.Error,
                    "Required parameter 'MY_FireRating' is missing on '900x2100'.",
                    "900x2100",
                    "MY_FireRating",
                    "Single Flush",
                    "Doors",
                    1),
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-missing-type-002-my_firerating",
                    EvidenceSeverity.Error,
                    "Required parameter 'MY_FireRating' is missing on '900x2200'.",
                    "900x2200",
                    "MY_FireRating",
                    "Single Flush",
                    "Doors",
                    1)),
            ComplianceReportProfileTestData.CreateDiagnosticCollection(
                CreateScopeDiagnostic("Doors", placedInstances: 2, typesChecked: 11, familiesChecked: 1)),
            fixEnabled: true,
            parameterDefaults: new Dictionary<string, string> { ["MY_FireRating"] = "EI60" });
    }

    private static DiagnosticRecord CreateScopeDiagnostic(
        string categoryName,
        int placedInstances,
        int typesChecked,
        int familiesChecked)
    {
        return new DiagnosticRecord
        {
            DiagnosticId = "validation-scope-doors",
            Timestamp = new DateTimeOffset(2026, 6, 19, 21, 0, 0, TimeSpan.Zero),
            Source = new DiagnosticSource
            {
                ComponentType = "ValidationPipeline",
                ComponentId = "validation-pipeline",
                Operation = "ValidationScope",
                Code = "ValidationScope.Category"
            },
            Category = DiagnosticCategory.Runtime,
            Severity = DiagnosticSeverity.Information,
            Message = "Validation scope",
            StructuredMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["scopeCategory"] = categoryName,
                ["validationLevel"] = "Type",
                ["familiesChecked"] = familiesChecked.ToString(),
                ["typesChecked"] = typesChecked.ToString(),
                ["objectsChecked"] = typesChecked.ToString(),
                ["placedInstances"] = placedInstances.ToString()
            }
        };
    }

    private static void AssertSectionExists(Contracts.Reports.Output.ReportOutput output, string sectionName)
    {
        Assert.Contains(output.Sections, section => section.Name == sectionName);
    }

    private static Contracts.Reports.Output.ReportSection GetSection(
        Contracts.Reports.Output.ReportOutput output,
        string sectionName)
    {
        return output.Sections.First(section => section.Name == sectionName);
    }
}
