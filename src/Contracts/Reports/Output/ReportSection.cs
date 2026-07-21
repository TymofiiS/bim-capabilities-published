namespace BIMCapabilities.Contracts.Reports.Output;

/// <summary>
/// A structured section within a prepared report output.
/// </summary>
public sealed record ReportSection
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public int Order { get; init; }

    public bool Required { get; init; }

    public ReportContent? Content { get; init; }
}
