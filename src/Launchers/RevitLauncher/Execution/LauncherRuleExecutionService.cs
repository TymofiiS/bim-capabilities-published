using BIMCapabilities.Composition.Logging;
using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Launchers.Revit.Diagnostics;
using BIMCapabilities.Launchers.Revit.Results;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Executes the MVP validation pipeline from the Revit launcher and persists generated reports.
/// </summary>
public sealed class LauncherRuleExecutionService
{
    private readonly IValidationPipeline _pipeline;
    private readonly ReportOutputWriter _reportWriter;
    private readonly IReportBrowserLauncher _reportBrowserLauncher;

    public LauncherRuleExecutionService()
        : this(new ValidationPipeline(), new ReportOutputWriter(), new ReportBrowserLauncher())
    {
    }

    internal LauncherRuleExecutionService(
        IValidationPipeline pipeline,
        ReportOutputWriter reportWriter,
        IReportBrowserLauncher reportBrowserLauncher)
    {
        _pipeline = pipeline;
        _reportWriter = reportWriter;
        _reportBrowserLauncher = reportBrowserLauncher;
    }

    public LauncherRuleExecutionResult Execute(LauncherRuleExecutionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var correlationId = request.CorrelationId ?? $"corr-revit-launcher-{Guid.NewGuid():N}";
        var executedAt = request.ExecutedAt ?? DateTimeOffset.UtcNow;
        var reportDirectory = LauncherPathResolver.ResolveReportDirectory(correlationId);

        var loader = new BimRuleLoader();
        var preLoadResult = loader.Load(request.RuleFilePath);
        var sharedParameterPath = ResolveSharedParameterPath(request, preLoadResult);

        ExecutionLogWriter? executionLog = null;
        string? executionLogPath = null;
        if (preLoadResult.Success && preLoadResult.Rule is not null)
        {
            executionLog = ExecutionLogWriter.CreateIfEnabled(
                preLoadResult.Rule.Report,
                reportDirectory,
                preLoadResult.Rule.Metadata.RuleId,
                correlationId,
                executedAt);
            executionLogPath = executionLog?.LogFilePath;
            executionLog?.WriteInformation("revit-launcher", $"Validation started. RuleFile={request.RuleFilePath}");
        }

        ValidationPipelineResult pipelineResult;
        try
        {
            pipelineResult = _pipeline.Execute(new ValidationPipelineRequest
            {
                RuleFilePath = request.RuleFilePath,
                FamilyProvider = request.FamilyProvider,
                SharedParameterFilePathOverride = sharedParameterPath,
                Scope = request.Scope,
                Environment = request.Environment,
                CorrelationId = correlationId,
                ExecutedAt = executedAt,
                ExecutionLog = executionLog,
                ProgressReporter = request.ProgressReporter
            });
        }
        catch (Exception exception)
        {
            executionLog?.WriteError("revit-launcher", $"Validation failed: {exception.Message}");
            return new LauncherRuleExecutionResult
            {
                Status = LauncherRuleExecutionStatus.ExecutionFailed,
                CorrelationId = correlationId,
                ExecutionLogPath = executionLogPath,
                ErrorMessage = LauncherDiagnosticFormatter.FormatExecutionFailure(exception)
            };
        }
        finally
        {
            executionLog?.WriteInformation("revit-launcher", "Validation launcher step finished.");
            executionLog?.Dispose();
        }

        if (!pipelineResult.LoadResult.Success || pipelineResult.LoadResult.Rule is null)
        {
            return new LauncherRuleExecutionResult
            {
                Status = LauncherRuleExecutionStatus.RuleLoadFailed,
                PipelineResult = pipelineResult,
                CorrelationId = correlationId,
                ExecutionLogPath = executionLogPath,
                ErrorMessage = LauncherDiagnosticFormatter.FormatLoadFailure(pipelineResult.LoadResult)
            };
        }

        if (!pipelineResult.RuleValidationSucceeded)
        {
            return new LauncherRuleExecutionResult
            {
                Status = LauncherRuleExecutionStatus.RuleValidationFailed,
                PipelineResult = pipelineResult,
                CorrelationId = correlationId,
                ExecutionLogPath = executionLogPath,
                ErrorMessage = LauncherDiagnosticFormatter.FormatValidationFailure(pipelineResult)
            };
        }

        var ruleId = pipelineResult.LoadResult.Rule.Metadata.RuleId;
        var reportPaths = _reportWriter.Write(
            reportDirectory,
            ruleId,
            pipelineResult.HtmlReport,
            pipelineResult.JsonReport);

        if (request.OpenHtmlReportInBrowser && reportPaths.HtmlReportPath is not null)
        {
            _reportBrowserLauncher.OpenHtmlReport(reportPaths.HtmlReportPath);
        }

        return new LauncherRuleExecutionResult
        {
            Status = LauncherRuleExecutionStatus.Completed,
            PipelineResult = pipelineResult,
            ReportDirectory = reportPaths.ReportDirectory,
            HtmlReportPath = reportPaths.HtmlReportPath,
            JsonReportPath = reportPaths.JsonReportPath,
            CorrelationId = correlationId,
            ExecutionLogPath = executionLogPath
        };
    }

    private static string? ResolveSharedParameterPath(
        LauncherRuleExecutionRequest request,
        BimRuleLoadResult? preLoadResult = null)
    {
        if (!string.IsNullOrWhiteSpace(request.SharedParameterFilePathOverride))
        {
            return request.SharedParameterFilePathOverride;
        }

        var loadResult = preLoadResult ?? new BimRuleLoader().Load(request.RuleFilePath);
        if (!loadResult.Success || loadResult.Rule is null)
        {
            return null;
        }

        return LauncherPathResolver.ResolveSharedParameterFilePath(
            request.RuleFilePath,
            loadResult.Rule,
            null);
    }
}
