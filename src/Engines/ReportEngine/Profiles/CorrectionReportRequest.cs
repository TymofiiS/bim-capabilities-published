namespace BIMCapabilities.Engines.Report.Profiles;

/// <summary>
/// Input for preparing a correction report after parameter fixes.
/// </summary>
public sealed record CorrectionReportRequest
{
    public required string RuleId { get; init; }

    public required string RuleName { get; init; }

    public required int ParametersAdded { get; init; }

    public required int ValuesAssigned { get; init; }

    public int NamesRenamed { get; init; }

    public required int AffectedTypes { get; init; }

    public int AffectedFamilies { get; init; }

    public int AffectedInstances { get; init; }

    public IReadOnlyList<string> DefaultValuesApplied { get; init; } = [];

    public DateTimeOffset GeneratedAt { get; init; }

    public string? CorrelationId { get; init; }
}
