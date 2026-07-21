using Autodesk.Revit.UI;
using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Launchers.Revit.Commands;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Executes the full launcher orchestration for a selected BIMRule file.
/// </summary>
public sealed class RuleLauncherWorkflow
{
    private readonly RuleLauncherExecutionContext _context;

    public RuleLauncherWorkflow(RuleLauncherExecutionContext context)
    {
        ArgumentGuard.ThrowIfNull(context);
        ArgumentGuard.ThrowIfNull(context.Application);
        ArgumentGuard.ThrowIfNull(context.Document);
        _context = context;
    }

    public RuleLauncherWorkflowResult ExecuteRuleWorkflow(string ruleFilePath)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(ruleFilePath);

        RuleLauncherReportReference? validationBeforeReport = null;
        RuleLauncherReportReference? fixReport = null;
        RuleLauncherReportReference? validationAfterReport = null;
        string? userMessage = null;

        var dialogState = new DialogState
        {
            Execution = RunValidation(ruleFilePath)
        };

        validationBeforeReport = MapReportReference(dialogState.Execution);

        if (!dialogState.Execution.Succeeded)
        {
            userMessage = dialogState.Execution.ErrorMessage ?? "Validation failed.";
            RuleDialogSupport.ShowError("Validation Failed", userMessage);
            return new RuleLauncherWorkflowResult
            {
                WorkflowResult = Result.Failed,
                UserMessage = userMessage,
                ValidationBeforeReport = validationBeforeReport
            };
        }

        LauncherSessionState.Store(ruleFilePath, dialogState.Execution.PipelineResult!);

        string? workflowError = null;
        ValidationCompletionDialog.ShowUntilClosed(
            () => dialogState.Execution,
            action =>
            {
                if (action == ValidationCompletionDialog.CompletionAction.ApplyAutomaticCorrection)
                {
                    var corrected = RuleFixExecution.ExecuteAutomaticCorrection(
                        _context.Application,
                        _context.Document,
                        ruleFilePath,
                        dialogState.Execution.PipelineResult!,
                        dialogState.Execution,
                        out var fixError);

                    if (corrected is null)
                    {
                        workflowError = fixError;
                        return false;
                    }

                    fixReport = MapFixReportReference(corrected);

                    var correctionAction = FixCompletionDialog.ShowUntilClosed(corrected);

                    if (correctionAction == FixCompletionDialog.CorrectionAction.RunValidationAgain)
                    {
                        dialogState.Execution = RunValidation(ruleFilePath);
                        if (!dialogState.Execution.Succeeded)
                        {
                            workflowError = dialogState.Execution.ErrorMessage;
                            return false;
                        }

                        LauncherSessionState.Store(ruleFilePath, dialogState.Execution.PipelineResult!);
                        validationAfterReport = MapReportReference(dialogState.Execution);
                    }
                    else
                    {
                        dialogState.Execution = corrected;
                    }

                    return correctionAction == FixCompletionDialog.CorrectionAction.RunValidationAgain;
                }

                return false;
            });

        if (!string.IsNullOrWhiteSpace(workflowError))
        {
            userMessage = workflowError;
        }

        return new RuleLauncherWorkflowResult
        {
            WorkflowResult = Result.Succeeded,
            UserMessage = userMessage,
            ValidationBeforeReport = validationBeforeReport,
            FixReport = fixReport,
            ValidationAfterReport = validationAfterReport
        };
    }

    private LauncherRuleExecutionResult RunValidation(string ruleFilePath)
    {
        CorrectionProgressDialog? progressDialog = null;

        try
        {
            progressDialog = CorrectionProgressDialog.Show(
                1,
                "Running Validation",
                "Preparing validation...",
                (nint)_context.Application.MainWindowHandle);
            var progressScope = new CorrectionProgressScope(progressDialog);
            var adapter = RevitAdapterDocumentFactory.CreateOperational(
                _context.Document,
                (current, total, message) => progressScope.Report(current, total, message));
            var executionService = new LauncherRuleExecutionService();

            return executionService.Execute(new LauncherRuleExecutionRequest
            {
                RuleFilePath = ruleFilePath,
                FamilyProvider = adapter.Families,
                Scope = new ExecutionScope
                {
                    ScopeType = "EntireModel",
                    TargetDescription = _context.Document.Title
                },
                Environment = new ExecutionEnvironment
                {
                    Platform = "Revit",
                    PlatformVersion = _context.Application.Application.VersionName ?? "2026",
                    ModelName = _context.Document.Title,
                    ModelIdentifier = _context.Document.PathName
                },
                CorrelationId = $"corr-revit-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                ExecutedAt = DateTimeOffset.UtcNow,
                OpenHtmlReportInBrowser = false,
                ProgressReporter = (current, total, message) => progressScope.Report(current, total, message)
            });
        }
        finally
        {
            progressDialog?.CloseDialog();
        }
    }

    private sealed class DialogState
    {
        public required LauncherRuleExecutionResult Execution { get; set; }
    }

    private static RuleLauncherReportReference? MapReportReference(LauncherRuleExecutionResult execution)
    {
        if (string.IsNullOrWhiteSpace(execution.HtmlReportPath) && string.IsNullOrWhiteSpace(execution.JsonReportPath))
        {
            return null;
        }

        return new RuleLauncherReportReference
        {
            HtmlReportPath = execution.HtmlReportPath,
            JsonReportPath = execution.JsonReportPath
        };
    }

    private static RuleLauncherReportReference? MapFixReportReference(LauncherRuleExecutionResult execution)
    {
        if (string.IsNullOrWhiteSpace(execution.CorrectionHtmlReportPath) && string.IsNullOrWhiteSpace(execution.CorrectionJsonReportPath))
        {
            return null;
        }

        return new RuleLauncherReportReference
        {
            HtmlReportPath = execution.CorrectionHtmlReportPath,
            JsonReportPath = execution.CorrectionJsonReportPath
        };
    }
}
