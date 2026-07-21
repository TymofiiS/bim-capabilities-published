namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Opens generated HTML reports in the default browser.
/// </summary>
public interface IReportBrowserLauncher
{
    void OpenHtmlReport(string htmlFilePath);
}
