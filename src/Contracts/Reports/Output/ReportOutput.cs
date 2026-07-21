namespace BIMCapabilities.Contracts.Reports.Output;

/// <summary>
/// Canonical renderer-neutral report structure consumed by output formatters.
/// </summary>
public sealed record ReportOutput
{
    public required string ReportId { get; init; }

    public required string Title { get; init; }

    public required string ProfileId { get; init; }

    public required IReadOnlyList<ReportSection> Sections { get; init; }

    public ReportMetadata? Metadata { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}
