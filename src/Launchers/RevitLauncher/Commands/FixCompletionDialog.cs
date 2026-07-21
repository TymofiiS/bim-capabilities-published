using System.Diagnostics;
using Autodesk.Revit.UI;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Commands;

internal static class FixCompletionDialog
{
    internal enum CorrectionAction
    {
        None,
        OpenCorrectionReport,
        OpenReportFolder,
        RunValidationAgain
    }

    internal static CorrectionAction ShowUntilClosed(LauncherRuleExecutionResult executionResult)
    {
        while (true)
        {
            var action = Show(executionResult);
            if (action == CorrectionAction.None)
            {
                return CorrectionAction.None;
            }

            switch (action)
            {
                case CorrectionAction.OpenCorrectionReport:
                    OpenFile(executionResult.CorrectionHtmlReportPath);
                    continue;
                case CorrectionAction.OpenReportFolder:
                    OpenFolder(executionResult.ReportDirectory);
                    continue;
            }

            return action;
        }
    }

    private static CorrectionAction Show(LauncherRuleExecutionResult executionResult)
    {
        var rule = executionResult.PipelineResult?.LoadResult.Rule;
        var ruleName = rule?.Metadata.Name
            ?? rule?.Metadata.RuleId
            ?? "BIM rule";

        var dialog = RuleDialogSupport.CreateResultTaskDialog(
            "Correction Complete",
            $"{ruleName} — Correction complete");

        var links = new List<(TaskDialogCommandLinkId LinkId, CorrectionAction Action)>();
        var nextLink = TaskDialogCommandLinkId.CommandLink1;

        if (!string.IsNullOrWhiteSpace(executionResult.CorrectionHtmlReportPath))
        {
            dialog.AddCommandLink(nextLink, "Open Correction Report");
            links.Add((nextLink, CorrectionAction.OpenCorrectionReport));
            nextLink++;
        }

        if (!string.IsNullOrWhiteSpace(executionResult.ReportDirectory))
        {
            dialog.AddCommandLink(nextLink, "Open Report Folder");
            links.Add((nextLink, CorrectionAction.OpenReportFolder));
            nextLink++;
        }

        dialog.AddCommandLink(nextLink, "Run Validation Again");
        links.Add((nextLink, CorrectionAction.RunValidationAgain));

        var dialogResult = dialog.Show();
        if (dialogResult == TaskDialogResult.Close || dialogResult == TaskDialogResult.Cancel)
        {
            return CorrectionAction.None;
        }

        foreach (var (linkId, action) in links)
        {
            if (dialogResult == (TaskDialogResult)linkId)
            {
                return action;
            }
        }

        return CorrectionAction.None;
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
