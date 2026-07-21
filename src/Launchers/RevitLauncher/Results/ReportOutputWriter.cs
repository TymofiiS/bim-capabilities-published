using BIMCapabilities.Contracts.Reports.Rendering;

namespace BIMCapabilities.Launchers.Revit.Results;

/// <summary>
/// Writes generated HTML and JSON reports to the BIMCapabilities temp directory.
/// </summary>
public sealed class ReportOutputWriter
{
    public ReportOutputPaths Write(
        string reportDirectory,
        string ruleId,
        HtmlRenderResult? htmlReport,
        JsonRenderResult? jsonReport)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(reportDirectory);
        ArgumentGuard.ThrowIfNullOrWhiteSpace(ruleId);

        Directory.CreateDirectory(reportDirectory);

        string? htmlPath = null;
        string? jsonPath = null;

        if (htmlReport is not null)
        {
            htmlPath = Path.Combine(reportDirectory, $"{ruleId}-report.html");
            File.WriteAllText(htmlPath, htmlReport.Html);
        }

        if (jsonReport is not null)
        {
            jsonPath = Path.Combine(reportDirectory, $"{ruleId}-report.json");
            File.WriteAllText(jsonPath, jsonReport.Json);
        }

        return new ReportOutputPaths
        {
            ReportDirectory = reportDirectory,
            HtmlReportPath = htmlPath,
            JsonReportPath = jsonPath
        };
    }

    public ReportOutputPaths WriteCorrection(
        string reportDirectory,
        string ruleId,
        HtmlRenderResult? htmlReport,
        JsonRenderResult? jsonReport)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(reportDirectory);
        ArgumentGuard.ThrowIfNullOrWhiteSpace(ruleId);

        Directory.CreateDirectory(reportDirectory);

        string? htmlPath = null;
        string? jsonPath = null;

        if (htmlReport is not null)
        {
            htmlPath = Path.Combine(reportDirectory, $"{ruleId}-correction-report.html");
            File.WriteAllText(htmlPath, htmlReport.Html);
        }

        if (jsonReport is not null)
        {
            jsonPath = Path.Combine(reportDirectory, $"{ruleId}-correction-report.json");
            File.WriteAllText(jsonPath, jsonReport.Json);
        }

        return new ReportOutputPaths
        {
            ReportDirectory = reportDirectory,
            HtmlReportPath = htmlPath,
            JsonReportPath = jsonPath
        };
    }
}

public sealed record ReportOutputPaths
{
    public required string ReportDirectory { get; init; }

    public string? HtmlReportPath { get; init; }

    public string? JsonReportPath { get; init; }
}
