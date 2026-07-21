using System.Diagnostics;
using Autodesk.Revit.UI;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Commands;

internal static class ValidationCompletionDialog
{
    internal enum CompletionAction
    {
        None,
        OpenValidationReport,
        OpenReportFolder,
        ApplyAutomaticCorrection
    }

    internal static void ShowUntilClosed(
        Func<LauncherRuleExecutionResult> getExecutionResult,
        Func<CompletionAction, bool>? actionHandler = null)
    {
        while (true)
        {
            var executionResult = getExecutionResult();
            var action = Show(executionResult);
            if (action == CompletionAction.None)
            {
                return;
            }

            if (actionHandler?.Invoke(action) == true)
            {
                continue;
            }

            ExecuteAction(action, executionResult);
        }
    }

    internal static CompletionAction Show(LauncherRuleExecutionResult executionResult)
    {
        var rule = executionResult.PipelineResult?.LoadResult.Rule;
        var ruleName = rule?.Metadata.Name
            ?? rule?.Metadata.RuleId
            ?? "BIM rule";
        var reportOutput = executionResult.PipelineResult?.ReportOutput;
        var summary = reportOutput?.Sections
            .FirstOrDefault(section => section.Name == "Compliance Summary")
            ?.Content?.StructuredData;

        var resultStatus = summary?.GetValueOrDefault("resultStatus") ?? "Complete";
        var issuesFound = summary?.GetValueOrDefault("issuesFound") ?? "0";
        var isPass = string.Equals(resultStatus, "Pass", StringComparison.OrdinalIgnoreCase);
        var canCorrect = RuleDialogSupport.CanApplyAutomaticCorrection(rule, resultStatus, issuesFound);

        var dialog = RuleDialogSupport.CreateResultTaskDialog(
            "Validation Complete",
            $"{ruleName} — {(isPass ? "PASS" : "FAIL")}");

        var links = new List<(TaskDialogCommandLinkId LinkId, CompletionAction Action)>();
        var nextLink = TaskDialogCommandLinkId.CommandLink1;

        if (!string.IsNullOrWhiteSpace(executionResult.HtmlReportPath))
        {
            dialog.AddCommandLink(nextLink, "Open Validation Report");
            links.Add((nextLink, CompletionAction.OpenValidationReport));
            nextLink++;
        }

        if (!string.IsNullOrWhiteSpace(executionResult.ReportDirectory))
        {
            dialog.AddCommandLink(nextLink, "Open Report Folder");
            links.Add((nextLink, CompletionAction.OpenReportFolder));
            nextLink++;
        }

        if (canCorrect)
        {
            dialog.AddCommandLink(nextLink, "Apply Automatic Correction");
            links.Add((nextLink, CompletionAction.ApplyAutomaticCorrection));
        }

        var dialogResult = dialog.Show();
        if (dialogResult == TaskDialogResult.Close || dialogResult == TaskDialogResult.Cancel)
        {
            return CompletionAction.None;
        }

        foreach (var (linkId, action) in links)
        {
            if (dialogResult == (TaskDialogResult)linkId)
            {
                return action;
            }
        }

        return CompletionAction.None;
    }

    internal static void ExecuteAction(CompletionAction action, LauncherRuleExecutionResult executionResult)
    {
        switch (action)
        {
            case CompletionAction.OpenValidationReport:
                OpenFile(executionResult.HtmlReportPath);
                break;
            case CompletionAction.OpenReportFolder:
                OpenFolder(executionResult.ReportDirectory);
                break;
        }
    }

    private static void OpenFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private static void OpenFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
