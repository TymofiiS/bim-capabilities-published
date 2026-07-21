using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Output;
using BIMCapabilities.Contracts.Reports.Profiles;
using BIMCapabilities.Engines.Report.Profiles;

namespace BIMCapabilities.Engines.Report.Tests;

public class ComplianceReportProfileTests
{
    private readonly ComplianceReportProfile _profile = new();

    [Fact]
    public void Compliance_report_profile_implements_required_interfaces()
    {
        Assert.IsAssignableFrom<IReportProfile>(_profile);
        Assert.IsAssignableFrom<IComplianceReportProfile>(_profile);
        Assert.Equal(ReportProfileType.Compliance, _profile.ProfileType);
        Assert.Equal("compliance-report-v1", _profile.Profile.ProfileId);
    }

    [Fact]
    public void Prepare_generates_report_output_with_required_sections()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreatePassingEvidence("evidence-pass-001"))));

        AssertSectionExists(output, "Compliance Summary");
        AssertSectionExists(output, "Project Impact");
        AssertSectionExists(output, "Business Impact");
        AssertSectionExists(output, "Validation Scope");
        AssertSectionExists(output, "Grouped Findings");
        AssertSectionExists(output, "Recommendations");
        AssertSectionExists(output, "Evidence");
        AssertSectionExists(output, "Diagnostics");
        Assert.Equal("compliance-report-v1", output.ProfileId);
    }

    [Fact]
    public void Prepare_handles_empty_evidence()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection()));

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("0", summary.StructuredData!["checkedObjects"]);
        Assert.Equal("0", summary.StructuredData!["passedObjects"]);
        Assert.Equal("0", summary.StructuredData!["failedObjects"]);
        Assert.Equal("100", summary.StructuredData!["compliancePercentage"]);
        Assert.Equal("Pass", summary.StructuredData!["resultStatus"]);
    }

    [Fact]
    public void Prepare_handles_single_violation()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "evidence-violation-001",
                    EvidenceSeverity.Error,
                    "Required parameter missing.",
                    "DR_HT_001",
                    "MY_FireRating"))));

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("1", summary.StructuredData!["failedObjects"]);
        Assert.Equal("1", summary.StructuredData!["issuesFound"]);
        Assert.Equal("Fail", summary.StructuredData!["resultStatus"]);

        var findings = GetSection(output, "Grouped Findings").Content!;
        Assert.Equal("1", findings.StructuredData!["issueGroupCount"]);
        Assert.Equal("Missing MY_FireRating", findings.StructuredData!["group[0].issueTitle"]);
        Assert.Equal("1", findings.StructuredData!["group[0].familyGroupCount"]);
        Assert.Equal("DR_HT_001", findings.StructuredData!["group[0].familyGroup[0].familyName"]);
        Assert.Contains("MY_FireRating", findings.StructuredData!["group[0].whyFailed"]);
        Assert.Equal("4", findings.StructuredData!["group[0].fixStepCount"]);
        Assert.Contains("Add the shared parameter MY_FireRating", findings.StructuredData!["group[0].fixStep[1]"]);
    }

    [Fact]
    public void Prepare_deduplicates_parameter_violations_for_same_object()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-missing-dr-001-my_firerating",
                    EvidenceSeverity.Error,
                    "Required parameter 'MY_FireRating' is missing.",
                    "DR_001",
                    "MY_FireRating"),
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-value-missingvalue-dr-001-my_firerating",
                    EvidenceSeverity.Error,
                    "Parameter 'MY_FireRating' is missing a required value.",
                    "DR_001",
                    "MY_FireRating"),
                ComplianceReportProfileTestData.CreateViolation(
                    "shared-parameter-missing-dr-001-my_firerating",
                    EvidenceSeverity.Error,
                    "Shared parameter 'MY_FireRating' failed validation.",
                    "DR_001",
                    "MY_FireRating"))));

        var summary = GetSection(output, "Compliance Summary").Content!;
        Assert.Equal("1", summary.StructuredData!["issuesFound"]);
        Assert.Equal("1", summary.StructuredData!["failedObjects"]);
    }

    [Fact]
    public void Prepare_groups_same_parameter_across_multiple_objects()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-missing-dr-001-my_firerating",
                    EvidenceSeverity.Error,
                    "Required parameter 'MY_FireRating' is missing.",
                    "DR_001",
                    "MY_FireRating"),
                ComplianceReportProfileTestData.CreateViolation(
                    "parameter-missing-dr-002-my_firerating",
                    EvidenceSeverity.Error,
                    "Required parameter 'MY_FireRating' is missing.",
                    "DR_002",
                    "MY_FireRating"))));

        var findings = GetSection(output, "Grouped Findings").Content!;
        Assert.Equal("1", findings.StructuredData!["issueGroupCount"]);
        Assert.Equal("2", findings.StructuredData!["group[0].count"]);
        Assert.Equal("2", findings.StructuredData!["group[0].familyGroupCount"]);
    }

    [Fact]
    public void Prepare_reports_instance_bound_value_findings_at_instance_scope()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateInstanceValueViolation(
                    "parameter-value-missingvalue-1663918-my_room",
                    "1663918",
                    "600 x 900mm",
                    "M_Window-Fixed",
                    "600 x 900mm",
                    "MY_Room"))));

        var findings = GetSection(output, "Grouped Findings").Content!;
        Assert.Equal("MY_Room missing value", findings.StructuredData!["group[0].issueTitle"]);
        Assert.Equal("1", findings.StructuredData!["group[0].affectedInstanceCount"]);
        Assert.Equal("0", findings.StructuredData!["group[0].affectedTypeCount"]);
        Assert.Equal("instance", findings.StructuredData!["group[0].validationScope"]);
        Assert.Equal("M_Window-Fixed", findings.StructuredData!["group[0].familyGroup[0].familyName"]);
        Assert.Equal("1", findings.StructuredData!["group[0].familyGroup[0].instanceCount"]);
        Assert.Contains("placed instance", findings.StructuredData!["group[0].whyFailed"], StringComparison.OrdinalIgnoreCase);

        var projectImpact = GetSection(output, "Project Impact").Content!;
        Assert.Equal("1", projectImpact.StructuredData!["projectImpactLine[1].value"]);
    }

    [Fact]
    public void Prepare_includes_diagnostics_section_for_json()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreatePassingEvidence("evidence-pass-001")),
            ComplianceReportProfileTestData.CreateDiagnosticCollection(
                ComplianceReportProfileTestData.CreateDiagnostic("diagnostic-001"),
                ComplianceReportProfileTestData.CreateDiagnostic("diagnostic-002"))));

        var diagnostics = GetSection(output, "Diagnostics").Content!;
        Assert.Equal("2", diagnostics.StructuredData!["totalDiagnostics"]);
        Assert.Equal(2, diagnostics.DiagnosticReferences!.Count);
        Assert.Equal("Technical", diagnostics.StructuredData!["audience"]);
    }

    [Fact]
    public void Prepare_generates_mvp_demo_compliance_report_structure()
    {
        var output = _profile.Prepare(ComplianceReportProfileTestData.CreateRequest(
            ComplianceReportProfileTestData.CreateEvidenceCollection(
                ComplianceReportProfileTestData.CreateViolation(
                    "evidence-001",
                    EvidenceSeverity.Error,
                    "Required shared parameter 'FireRating' is missing.",
                    "DR_HT_001",
                    "FireRating"),
                ComplianceReportProfileTestData.CreatePassingEvidence("evidence-002"))));

        Assert.Equal("Openings Compliance Report", output.Title);
        Assert.Equal(ComplianceReportProfileTestData.RuleId, output.Metadata!.RuleId);
        Assert.StartsWith("report-STD-ARC-OPENINGS-V01", output.ReportId);
        Assert.Equal(new DateTimeOffset(2026, 6, 19, 21, 0, 0, TimeSpan.Zero), output.GeneratedAt);
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
