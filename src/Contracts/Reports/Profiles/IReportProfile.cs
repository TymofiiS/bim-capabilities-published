using BIMCapabilities.Contracts.Reports.Output;

namespace BIMCapabilities.Contracts.Reports.Profiles;

/// <summary>
/// Contract for a report profile that prepares renderer-neutral output.
/// </summary>
public interface IReportProfile
{
    ReportProfileType ProfileType { get; }

    ReportProfile Profile { get; }

    ReportOutput Prepare(ReportProfileRequest request);
}
