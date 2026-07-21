namespace BIMCapabilities.Contracts.Reports.Profiles;

/// <summary>
/// Identifies a report profile and its reporting intent.
/// </summary>
public sealed record ReportProfile
{
    public required string ProfileId { get; init; }

    public required string Name { get; init; }

    public required ReportProfileType ProfileType { get; init; }

    public string? Description { get; init; }

    public required ReportProfileDefinition Definition { get; init; }
}
