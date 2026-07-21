namespace BIMCapabilities.Contracts.Reports.Profiles;

/// <summary>
/// Describes a section required or optional within a report profile.
/// </summary>
public sealed record ReportProfileSection
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public bool Required { get; init; }

    public int Order { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
