using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Tests.Fixtures;

internal sealed class RecordingReportBrowserLauncher : IReportBrowserLauncher
{
    public string? OpenedHtmlReportPath { get; private set; }

    public void OpenHtmlReport(string htmlFilePath)
    {
        OpenedHtmlReportPath = htmlFilePath;
    }
}
