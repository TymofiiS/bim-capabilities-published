using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Engines.Report.Profiles;

internal static class CorrectionReportProfileDefinition
{
    internal const string ProfileId = "correction-report-v1";

    internal static ReportProfile Create()
    {
        return new ReportProfile
        {
            ProfileId = ProfileId,
            Name = "Correction Report",
            ProfileType = ReportProfileType.Fix,
            Description = "Summarizes parameter corrections applied by executable knowledge.",
            Definition = new ReportProfileDefinition
            {
                ProfileType = ReportProfileType.Fix,
                Sections =
                [
                    new ReportProfileSection
                    {
                        Name = CorrectionReportProfileSections.CorrectionSummary,
                        Required = true,
                        Order = 1,
                        Description = "Summarizes parameters added and values assigned."
                    }
                ],
                Configuration = new ReportProfileConfiguration
                {
                    PresentationIntent = "CoordinatorDelivery"
                }
            }
        };
    }
}

internal static class CorrectionReportProfileSections
{
    internal const string CorrectionSummary = "Correction Summary";
}
