using BIMCapabilities.Contracts.Reports.Output;

namespace BIMCapabilities.Contracts.Reports.Rendering;

/// <summary>
/// Contract for rendering prepared report output into JSON.
/// </summary>
public interface IJsonReportRenderer
{
    string Format { get; }

    JsonRenderResult Render(ReportOutput report);
}
