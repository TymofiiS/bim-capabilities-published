using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Composition.Logging;
using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Commands;

/// <summary>
/// Applies automatic correction from a completed validation run (dialog-driven only).
/// </summary>
internal static class RuleFixExecution
{
    internal static LauncherRuleExecutionResult? ExecuteAutomaticCorrection(
        UIApplication application,
        Document document,
        string ruleFilePath,
        ValidationPipelineResult validationResult,
        LauncherRuleExecutionResult validationExecution,
        out string? errorMessage)
    {
        errorMessage = null;
        var rule = validationResult.LoadResult.Rule;
        if (rule?.Execution.FixEnabled != true)
        {
            errorMessage = "Automatic correction is not enabled for this rule.";
            RuleDialogSupport.ShowError("Automatic Correction Unavailable", errorMessage);
            return null;
        }

        var adapter = RevitAdapterDocumentFactory.CreateOperational(document);
        var fixService = new LauncherRuleFixExecutionService();
        CorrectionProgressDialog? progressDialog = null;
        CorrectionProgressScope? progressScope = null;
        ExecutionLogWriter? executionLog = ExecutionLogWriter.OpenForAppend(validationExecution.ExecutionLogPath);

        LauncherRuleFixExecutionResult fixResult;
        try
        {
            progressDialog = CorrectionProgressDialog.Show(1, (nint)application.MainWindowHandle);
            progressScope = new CorrectionProgressScope(progressDialog);
            progressScope.Report(0, 1, "Applying automatic correction...");

            var correlationId = validationExecution.CorrelationId
                ?? $"corr-fix-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

            fixResult = fixService.Execute(new LauncherRuleFixExecutionRequest
            {
                Document = document,
                RuleFilePath = ruleFilePath,
                ValidationResult = validationResult,
                FamilyProvider = adapter.Families,
                Scope = new ExecutionScope
                {
                    ScopeType = "EntireModel",
                    TargetDescription = document.Title
                },
                Environment = new ExecutionEnvironment
                {
                    Platform = "Revit",
                    PlatformVersion = application.Application.VersionName ?? "2026",
                    ModelName = document.Title,
                    ModelIdentifier = document.PathName
                },
                CorrelationId = correlationId,
                ExecutedAt = DateTimeOffset.UtcNow,
                UserApprovedModification = true,
                ProgressScope = progressScope,
                ExecutionLog = executionLog
            });
        }
        finally
        {
            executionLog?.WriteInformation("revit-launcher", "Fix launcher step finished.");
            executionLog?.Dispose();
            progressDialog?.CloseDialog();
        }

        if (!fixResult.Succeeded)
        {
            errorMessage = fixResult.ErrorMessage ?? "Automatic correction failed.";
            RuleDialogSupport.ShowError("Automatic Correction Failed", errorMessage);
            return null;
        }

        return new LauncherRuleExecutionResult
        {
            Status = LauncherRuleExecutionStatus.Completed,
            PipelineResult = validationResult,
            ReportDirectory = fixResult.ReportDirectory ?? validationExecution.ReportDirectory,
            HtmlReportPath = validationExecution.HtmlReportPath,
            JsonReportPath = validationExecution.JsonReportPath,
            CorrectionHtmlReportPath = fixResult.CorrectionHtmlReportPath,
            CorrectionJsonReportPath = fixResult.CorrectionJsonReportPath,
            FixSummary = fixResult.FixExecutionResult,
            CorrelationId = validationExecution.CorrelationId,
            ExecutionLogPath = validationExecution.ExecutionLogPath
        };
    }
}
