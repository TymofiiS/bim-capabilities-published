using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Engines.Report.Profiles;

/// <summary>
/// Default compliance report profile definition for the MVP demo.
/// </summary>
internal static class ComplianceReportProfileDefinition
{
    internal const string ProfileId = "compliance-report-v1";

    internal static ReportProfile Create()
    {
        return new ReportProfile
        {
            ProfileId = ProfileId,
            Name = "Compliance Report",
            ProfileType = ReportProfileType.Compliance,
            Description = "Pass and fail compliance reporting intent for the MVP demo.",
            Definition = new ReportProfileDefinition
            {
                ProfileType = ReportProfileType.Compliance,
                Sections =
                [
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.ComplianceSummary,
                        Required = true,
                        Order = 1,
                        Description = "Summarizes overall compliance results."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.ProjectImpact,
                        Required = true,
                        Order = 2,
                        Description = "Shows placed and affected project objects."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.BusinessImpact,
                        Required = true,
                        Order = 3,
                        Description = "Summarizes correction scope in business terms."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.RootCause,
                        Required = true,
                        Order = 4,
                        Description = "Explains why validation failed and what correction will do."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.AutomaticCorrectionPreview,
                        Required = false,
                        Order = 5,
                        Description = "Preview of deterministic automatic correction."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.ValidationScope,
                        Required = true,
                        Order = 6,
                        Description = "Explains what was validated and at which level."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.GroupedFindings,
                        Required = true,
                        Order = 7,
                        Description = "Groups compliance findings by issue type."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.Recommendations,
                        Required = false,
                        Order = 8,
                        Description = "Suggested actions to resolve compliance issues."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.Evidence,
                        Required = false,
                        Order = 9,
                        Description = "Technical evidence referenced by the report."
                    },
                    new ReportProfileSection
                    {
                        Name = ComplianceReportProfileSections.Diagnostics,
                        Required = false,
                        Order = 10,
                        Description = "Technical runtime diagnostics related to the report."
                    }
                ],
                Configuration = new ReportProfileConfiguration
                {
                    EvidenceSelectionStrategy = "IncludeComplianceEvidence",
                    AggregationStrategy = "GroupByIssue",
                    SummaryStrategy = "ComplianceSummary",
                    PresentationIntent = "CoordinatorDelivery"
                }
            }
        };
    }
}

internal static class ComplianceReportProfileSections
{
    internal const string ComplianceSummary = "Compliance Summary";

    internal const string ProjectImpact = "Project Impact";

    internal const string BusinessImpact = "Business Impact";

    internal const string RootCause = "Root Cause";

    internal const string AutomaticCorrectionPreview = "Automatic Correction Preview";

    internal const string ValidationScope = "Validation Scope";

    internal const string GroupedFindings = "Grouped Findings";

    internal const string Recommendations = "Recommendations";

    internal const string Violations = "Grouped Findings";

    internal const string Evidence = "Evidence";

    internal const string Diagnostics = "Diagnostics";
}
