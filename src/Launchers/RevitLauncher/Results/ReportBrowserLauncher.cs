using System.Diagnostics;
using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Results;

/// <summary>
/// Opens generated HTML reports using the operating system default browser.
/// </summary>
public sealed class ReportBrowserLauncher : IReportBrowserLauncher
{
    public void OpenHtmlReport(string htmlFilePath)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(htmlFilePath);

        if (!File.Exists(htmlFilePath))
        {
            throw new FileNotFoundException("HTML report file was not found.", htmlFilePath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = htmlFilePath,
            UseShellExecute = true
        });
    }
}
