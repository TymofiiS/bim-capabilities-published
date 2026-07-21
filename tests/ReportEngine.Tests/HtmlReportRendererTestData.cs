using BIMCapabilities.Contracts.Reports.Output;

namespace BIMCapabilities.Engines.Report.Tests;

internal static class HtmlReportRendererTestData
{
    internal static ReportOutput CreateEmptyReport()
    {
        return new ReportOutput
        {
            ReportId = "report-empty",
            Title = "Empty Report",
            ProfileId = "compliance-report-v1",
            GeneratedAt = new DateTimeOffset(2026, 6, 19, 22, 0, 0, TimeSpan.Zero),
            Sections = []
        };
    }

    internal static ReportOutput CreateSimpleReport()
    {
        return new ReportOutput
        {
            ReportId = "report-simple",
            Title = "Simple Report",
            ProfileId = "compliance-report-v1",
            GeneratedAt = new DateTimeOffset(2026, 6, 19, 22, 1, 0, TimeSpan.Zero),
            Metadata = new ReportMetadata
            {
                RuleId = "STD-ARC-OPENINGS-V01",
                GeneratedBy = "HtmlReportRendererTests"
            },
            Sections =
            [
                new ReportSection
                {
                    Name = "Summary",
                    Description = "Simple summary section.",
                    Order = 1,
                    Required = true,
                    Content = new ReportContent
                    {
                        Text = "All checks passed."
                    }
                }
            ]
        };
    }

    internal static ReportOutput CreateMultiSectionReport()
    {
        return new ReportOutput
        {
            ReportId = "report-multi",
            Title = "Multi Section Report",
            ProfileId = "compliance-report-v1",
            GeneratedAt = new DateTimeOffset(2026, 6, 19, 22, 2, 0, TimeSpan.Zero),
            Sections =
            [
                new ReportSection
                {
                    Name = "Section B",
                    Order = 2,
                    Required = true,
                    Content = new ReportContent { Text = "Second section." }
                },
                new ReportSection
                {
                    Name = "Section A",
                    Order = 1,
                    Required = true,
                    Content = new ReportContent { Text = "First section." }
                }
            ]
        };
    }
}
