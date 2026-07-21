using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Loading;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Input for executing a BIMRule validation workflow from the Revit launcher.
/// </summary>
public sealed record LauncherRuleExecutionRequest
{
    public required string RuleFilePath { get; init; }

    public required IFamilyProvider FamilyProvider { get; init; }

    public required ExecutionScope Scope { get; init; }

    public required ExecutionEnvironment Environment { get; init; }

    public string? SharedParameterFilePathOverride { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public bool OpenHtmlReportInBrowser { get; init; } = true;

    public Action<int, int, string>? ProgressReporter { get; init; }
}
