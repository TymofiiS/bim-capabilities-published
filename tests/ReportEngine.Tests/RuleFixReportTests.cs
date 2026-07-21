using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Profiles;

namespace BIMCapabilities.Engines.Report.Tests;

public class RuleFixReportTests
{
    private readonly ComplianceReportProfile _profile = new();

    [Fact]
    public void Prepare_reflects_parameter_fill_rules_and_prefix_fix_in_report_text()
    {
        var output = _profile.Prepare(CreateOpeningsRuleRequest());

        var rootCause = output.Sections.First(section => section.Name == "Root Cause").Content!;
        Assert.Contains("Model is empty", rootCause.Text, StringComparison.Ordinal);
        Assert.Contains("WD_", rootCause.Text, StringComparison.Ordinal);

        var preview = output.Sections.First(section => section.Name == "Automatic Correction Preview").Content!;
        Assert.Contains("Fill Model from family type name", preview.Text, StringComparison.Ordinal);
        Assert.Contains("Rename Windows types to add WD_ prefix", preview.Text, StringComparison.Ordinal);
        Assert.Equal("Model=from:FamilyTypeName", preview.StructuredData!["correctionPreviewLine[1].value"]);

        var groupedFindings = output.Sections.First(section => section.Name == "Grouped Findings").Content!;
        Assert.Equal("2", groupedFindings.StructuredData!["issueGroupCount"]);
        Assert.Equal("Naming standard not met", groupedFindings.StructuredData!["group[1].issueTitle"]);
        Assert.Contains(
            "Apply Automatic Correction",
            groupedFindings.StructuredData!["group[0].fixStep[0]"],
            StringComparison.Ordinal);
        Assert.Contains(
            "family type name",
            groupedFindings.StructuredData!["group[0].fixStep[1]"],
            StringComparison.Ordinal);
        Assert.Contains(
            "WD_ prefix",
            groupedFindings.StructuredData!["group[1].fixStep[1]"],
            StringComparison.Ordinal);
    }

    private static ReportProfileRequest CreateOpeningsRuleRequest()
    {
        return ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-value-missingvalue-door-001-model",
                    EvidenceSeverity.Error,
                    "Parameter 'Model' on 900 x 2100mm is missing a required value.",
                    "900 x 2100mm",
                    "Model",
                    "Single Flush",
                    "Doors",
                    0),
                CreateNamingViolation(
                    "prefix-missingprefix-window-001",
                    "1200 x 1500mm",
                    "Casement",
                    "Windows")),
            ComplianceReportProfileTestData.CreateDiagnosticCollection(
                CreateScopeDiagnostic("Doors", typesChecked: 1, familiesChecked: 1),
                CreateScopeDiagnostic("Windows", typesChecked: 1, familiesChecked: 1)),
            fixEnabled: true,
            parameterFillRules: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Model"] = "from:FamilyTypeName"
            },
            categoryFixConfiguration: new Dictionary<string, ReportCategoryFixConfiguration>(StringComparer.OrdinalIgnoreCase)
            {
                ["Windows"] = new ReportCategoryFixConfiguration
                {
                    RequiredPrefix = "WD_",
                    PrefixFixScope = PrefixFixScope.Type
                }
            },
            ruleName: "Company Openings - Model Parameter & WD_ Prefix");
    }

    private static EvidenceRecord CreateNamingViolation(
        string evidenceId,
        string targetName,
        string familyName,
        string categoryName)
    {
        return new EvidenceRecord
        {
            EvidenceId = evidenceId,
            Timestamp = new DateTimeOffset(2026, 6, 22, 14, 0, 0, TimeSpan.Zero),
            Source = new EvidenceSource
            {
                EngineId = "naming-engine",
                AtomId = "naming.prefix.validation",
                RuleId = ComplianceReportProfileTestData.RuleId,
                CapabilityId = "naming.prefix.validation"
            },
            Target = new EvidenceTarget
            {
                TargetType = "familyType",
                TargetId = targetName,
                TargetName = targetName,
                TargetSetDescription = categoryName
            },
            Category = EvidenceCategory.Compliance,
            Severity = EvidenceSeverity.Error,
            Message = $"Object name '{targetName}' is missing a required prefix.",
            StructuredData = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["typeName"] = targetName,
                ["familyName"] = familyName,
                ["categoryName"] = categoryName
            }
        };
    }

    private static DiagnosticRecord CreateScopeDiagnostic(
        string categoryName,
        int typesChecked,
        int familiesChecked)
    {
        return new DiagnosticRecord
        {
            DiagnosticId = $"validation-scope-{categoryName.ToLowerInvariant()}",
            Timestamp = new DateTimeOffset(2026, 6, 22, 14, 0, 0, TimeSpan.Zero),
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
                ["placedInstances"] = "0"
            }
        };
    }
}
