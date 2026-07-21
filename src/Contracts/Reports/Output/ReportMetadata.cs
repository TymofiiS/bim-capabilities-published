namespace BIMCapabilities.Contracts.Reports.Output;

/// <summary>
/// Metadata describing a prepared report output.
/// </summary>
public sealed record ReportMetadata
{
    public string? RuleId { get; init; }

    public string? ProfileId { get; init; }

    public string? CorrelationId { get; init; }

    public string? GeneratedBy { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}
