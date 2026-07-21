using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Reports.Profiles;

/// <summary>
/// Input for preparing a report profile output.
/// </summary>
public sealed record ReportProfileRequest
{
    public required string RuleId { get; init; }

    public required string ReportTitle { get; init; }

    public EvidenceCollection? Evidence { get; init; }

    public DiagnosticCollection? Diagnostics { get; init; }

    public ExecutionScope? ExecutionScope { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }

    public bool FixEnabled { get; init; }

    public string? RuleName { get; init; }

    public IReadOnlyDictionary<string, string>? ParameterDefaults { get; init; }

    public IReadOnlyDictionary<string, string>? ParameterFillRules { get; init; }

    public IReadOnlyDictionary<string, ReportCategoryFixConfiguration>? CategoryFixConfiguration { get; init; }
}
