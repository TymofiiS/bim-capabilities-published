using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;

namespace BIMCapabilities.Engines.Report.Tests;

public class ValidationScopeReportTests
{
    private readonly ComplianceReportProfile _profile = new();
    private readonly HtmlReportRenderer _htmlRenderer = new();

    [Fact]
    public void Prepare_includes_validation_scope_section_with_category_counts()
    {
        var output = _profile.Prepare(CreateRequestWithScope(
            CreateScopeDiagnostic("Doors", typesChecked: 19, familiesChecked: 5, objectsChecked: 19),
            CreateScopeDiagnostic("Windows", typesChecked: 12, familiesChecked: 3, objectsChecked: 12)));

        var scope = output.Sections.First(section => section.Name == "Validation Scope").Content!;
        Assert.Equal("Project", scope.StructuredData!["modelScope"]);
        Assert.Equal("Type", scope.StructuredData!["validationLevel"]);
        Assert.Contains("family types", scope.StructuredData!["whyCountsDiffer"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("19", scope.StructuredData!["scopeLine[1].value"]);
        Assert.Equal("Door Types Checked", scope.StructuredData!["scopeLine[1].label"]);
    }

    [Fact]
    public void Prepare_uses_scope_totals_for_checked_objects_on_pass_reports()
    {
        var output = _profile.Prepare(CreateRequestWithScope(
            CreateScopeDiagnostic("Doors", typesChecked: 19, familiesChecked: 1, objectsChecked: 19)));

        var summary = output.Sections.First(section => section.Name == "Compliance Summary").Content!;
        Assert.Equal("19", summary.StructuredData!["checkedObjects"]);
        Assert.Equal("19", summary.StructuredData!["passedObjects"]);
        Assert.Equal("Pass", summary.StructuredData!["resultStatus"]);
    }

    [Fact]
    public void Html_renderer_shows_validation_scope_and_why_counts_differ()
    {
        var report = _profile.Prepare(CreateRequestWithScope(
            CreateScopeDiagnostic("Doors", typesChecked: 19, familiesChecked: 1, objectsChecked: 19)));

        var html = _htmlRenderer.Render(report).Html;

        Assert.Contains("<h2>Validation Scope</h2>", html);
        Assert.Contains("Door Types Checked", html);
        Assert.Contains("19", html);
        Assert.Contains("Why Counts Differ", html);
        Assert.Contains("family types", html, StringComparison.OrdinalIgnoreCase);
    }

    private static ReportProfileRequest CreateRequestWithScope(params DiagnosticRecord[] scopeDiagnostics)
    {
        return ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(),
            ComplianceReportProfileTestData.CreateDiagnosticCollection(scopeDiagnostics))
        with
        {
            ExecutionScope = new ExecutionScope
            {
                ScopeType = "EntireModel",
                TargetDescription = "Demo Project"
            }
        };
    }

    private static DiagnosticRecord CreateScopeDiagnostic(
        string categoryName,
        int typesChecked,
        int familiesChecked,
        int objectsChecked)
    {
        return new DiagnosticRecord
        {
            DiagnosticId = $"validation-scope-{categoryName.ToLowerInvariant()}",
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
            Message = $"Validation scope for {categoryName}",
            StructuredMetadata = new Dictionary<string, string>
            {
                ["scopeCategory"] = categoryName,
                ["modelScope"] = "EntireModel",
                ["validationLevel"] = "Type",
                ["familiesChecked"] = familiesChecked.ToString(),
                ["typesChecked"] = typesChecked.ToString(),
                ["objectsChecked"] = objectsChecked.ToString()
            }
        };
    }
}
