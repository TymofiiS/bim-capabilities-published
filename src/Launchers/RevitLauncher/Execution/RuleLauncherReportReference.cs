namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// File paths for a launcher-generated report pair.
/// </summary>
public sealed record RuleLauncherReportReference
{
    public string? HtmlReportPath { get; init; }

    public string? JsonReportPath { get; init; }
}
