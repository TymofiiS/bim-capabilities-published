using Autodesk.Revit.UI;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Public outcome of the reusable launcher workflow.
/// </summary>
public sealed record RuleLauncherWorkflowResult
{
    public required Result WorkflowResult { get; init; }

    public string? UserMessage { get; init; }

    public RuleLauncherReportReference? ValidationBeforeReport { get; init; }

    public RuleLauncherReportReference? FixReport { get; init; }

    public RuleLauncherReportReference? ValidationAfterReport { get; init; }

    public bool Succeeded => WorkflowResult == Result.Succeeded;
}
