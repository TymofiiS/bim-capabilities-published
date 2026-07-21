namespace BIMCapabilities.Contracts.Rules;

/// <summary>
/// Defines reporting intent for rule execution.
/// </summary>
public sealed record BimRuleReport
{
    public bool GenerateHtmlReport { get; init; } = true;

    public bool GenerateJsonReport { get; init; } = true;

    public bool IncludeEvidence { get; init; } = true;

    public bool EnableExecutionLog { get; init; }

    public string? ReportTitle { get; init; }

    public string? ComplianceSummaryProfile { get; init; }

    public string? ResultGrouping { get; init; }
}
