using BIMCapabilities.Contracts.Reports.Output;

namespace BIMCapabilities.Contracts.Reports.Rendering;

/// <summary>
/// Contract for rendering prepared report output into a specific format.
/// </summary>
public interface IReportRenderer
{
    string Format { get; }

    HtmlRenderResult Render(ReportOutput report);
}
