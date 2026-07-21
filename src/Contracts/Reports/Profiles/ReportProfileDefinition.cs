namespace BIMCapabilities.Contracts.Reports.Profiles;

/// <summary>
/// Defines the sections and configuration for a report profile type.
/// </summary>
public sealed record ReportProfileDefinition
{
    public required ReportProfileType ProfileType { get; init; }

    public required IReadOnlyList<ReportProfileSection> Sections { get; init; }

    public ReportProfileConfiguration? Configuration { get; init; }
}
