using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Commands;

/// <summary>
/// Revit ribbon command: select a BIM rule, validate, review results, and apply correction from the result dialog.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class RuleExecutionCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        ArgumentGuard.ThrowIfNull(commandData);

        var uiDocument = commandData.Application.ActiveUIDocument;
        if (uiDocument?.Document is null)
        {
            message = "Open a Revit model before running a BIM rule.";
            return Result.Failed;
        }

        var ruleFilePath = SelectRuleFile(commandData);
        if (ruleFilePath is null)
        {
            return Result.Cancelled;
        }

        var launcherContext = new RuleLauncherExecutionContext(commandData.Application, uiDocument.Document);
        var workflow = new RuleLauncherWorkflow(launcherContext);
        var result = workflow.ExecuteRuleWorkflow(ruleFilePath);

        message = result.UserMessage ?? message;

        return result.WorkflowResult;
    }

    private string? SelectRuleFile(ExternalCommandData commandData)
    {
        var revitOwnerHandle = (nint)commandData.Application.MainWindowHandle;
        var picker = new WinFormsRuleFilePicker();
        return picker.PickRuleFile(GetDefaultRuleDirectory(), revitOwnerHandle);
    }

    private static string GetDefaultRuleDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "BIMCapabilities",
                "Rules"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
