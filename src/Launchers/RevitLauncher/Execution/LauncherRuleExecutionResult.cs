using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Outcome of a Revit launcher validation execution.
/// </summary>
public sealed record LauncherRuleExecutionResult
{
    public required LauncherRuleExecutionStatus Status { get; init; }

    public ValidationPipelineResult? PipelineResult { get; init; }

    public string? HtmlReportPath { get; init; }

    public string? JsonReportPath { get; init; }

    public string? CorrectionHtmlReportPath { get; init; }

    public string? CorrectionJsonReportPath { get; init; }

    public LauncherFixExecutionResult? FixSummary { get; init; }

    public string? ReportDirectory { get; init; }

    public string? ExecutionLogPath { get; init; }

    public string? CorrelationId { get; init; }

    public string? ErrorMessage { get; init; }

    public bool Succeeded => Status == LauncherRuleExecutionStatus.Completed;
}

/// <summary>
/// Status codes for launcher validation execution.
/// </summary>
public enum LauncherRuleExecutionStatus
{
    Completed,
    RuleLoadFailed,
    RuleValidationFailed,
    ExecutionFailed
}
