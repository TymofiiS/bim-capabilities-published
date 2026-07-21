using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Contracts.Tests;

internal static class ReportProfileTestData
{
    internal static ReportProfile CreateComplianceProfile()
    {
        return new ReportProfile
        {
            ProfileId = "compliance-report-v1",
            Name = "Compliance Report",
            ProfileType = ReportProfileType.Compliance,
            Description = "Pass and fail compliance reporting intent.",
            Definition = CreateDefinition(
                ReportProfileType.Compliance,
                [
                    CreateSection("Compliance Summary", required: true, order: 1),
                    CreateSection("Violations", required: true, order: 2),
                    CreateSection("Evidence", required: true, order: 3),
                    CreateSection("Diagnostics", required: false, order: 4)
                ],
                new ReportProfileConfiguration
                {
                    EvidenceSelectionStrategy = "IncludeComplianceEvidence",
                    AggregationStrategy = "GroupBySeverity",
                    SummaryStrategy = "ComplianceSummary",
                    PresentationIntent = "HighlightViolations"
                })
        };
    }

    internal static ReportProfileDefinition CreateDefinition(
        ReportProfileType profileType,
        IReadOnlyList<ReportProfileSection> sections,
        ReportProfileConfiguration? configuration = null)
    {
        return new ReportProfileDefinition
        {
            ProfileType = profileType,
            Sections = sections,
            Configuration = configuration
        };
    }

    internal static ReportProfileSection CreateSection(string name, bool required, int order, string? description = null)
    {
        return new ReportProfileSection
        {
            Name = name,
            Description = description,
            Required = required,
            Order = order
        };
    }

    internal static IReadOnlyList<ReportProfileType> ApprovedProfileTypes()
    {
        return
        [
            ReportProfileType.Compliance,
            ReportProfileType.Validation,
            ReportProfileType.Fix,
            ReportProfileType.Audit,
            ReportProfileType.KnowledgeGap,
            ReportProfileType.Optimization
        ];
    }
}
