using BIMCapabilities.Composition.Fix;
using BIMCapabilities.Composition.Logging;
using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Execution.Logging;
using BIMCapabilities.Contracts.Reports.Rendering;
using BIMCapabilities.Engines.Report.Profiles;
using BIMCapabilities.Engines.Report.Rendering;
using BIMCapabilities.Launchers.Revit.Commands;
using BIMCapabilities.Launchers.Revit.Results;

namespace BIMCapabilities.Launchers.Revit.Execution;

/// <summary>
/// Executes parameter and naming fixes from validation findings and generates correction reports.
/// </summary>
public sealed class LauncherRuleFixExecutionService
{
    private readonly FixPipeline _fixPipeline = new();
    private readonly LauncherNamingFixExecutor _namingFixExecutor = new();
    private readonly LauncherParameterFixExecutor _parameterFixExecutor = new();
    private readonly ReportOutputWriter _reportWriter = new();

    public LauncherRuleFixExecutionResult Execute(LauncherRuleFixExecutionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var validationResult = request.ValidationResult;
        var rule = validationResult.LoadResult.Rule;
        if (rule is null)
        {
            return LauncherRuleFixExecutionResult.Failed("Rule must be loaded before fixes can run.");
        }

        if (rule.Execution.RequireUserApprovalBeforeModification
            && !request.UserApprovedModification)
        {
            return LauncherRuleFixExecutionResult.Failed("User approval is required before applying fixes.");
        }

        var fixBuildResult = _fixPipeline.BuildWriteRequests(new FixPipelineRequest
        {
            ValidationResult = validationResult,
            RuleFilePath = request.RuleFilePath,
            SharedParameterFilePathOverride = request.SharedParameterFilePathOverride,
            Scope = request.Scope,
            CorrelationId = request.CorrelationId,
            ExecutedAt = request.ExecutedAt
        });

        if (!fixBuildResult.Succeeded)
        {
            ExecutionLogSupport.WriteFixFailed(request.ExecutionLog, fixBuildResult.ErrorMessage ?? "Fix preparation failed.");
            return LauncherRuleFixExecutionResult.Failed(fixBuildResult.ErrorMessage ?? "Fix preparation failed.");
        }

        var namingRequests = fixBuildResult.WriteRequests
            .Where(writeRequest => writeRequest.RequestType is WriteRequestType.RenameFamily or WriteRequestType.RenameType)
            .ToArray();
        var parameterRequests = fixBuildResult.WriteRequests
            .Where(writeRequest => writeRequest.RequestType is WriteRequestType.ParameterCreate or WriteRequestType.ParameterUpdate)
            .ToArray();

        ExecutionLogSupport.WriteFixStarted(request.ExecutionLog, fixBuildResult.WriteRequests.Count);
        request.ExecutionLog?.WriteInformation("revit-launcher", "Automatic correction started in Revit.");

        var namesRenamed = 0;
        if (namingRequests.Length > 0)
        {
            var namingResult = _namingFixExecutor.Execute(
                request.Document,
                namingRequests,
                request.ExecutionLog,
                request.ProgressScope);

            if (!namingResult.Succeeded)
            {
                ExecutionLogSupport.WriteFixFailed(request.ExecutionLog, namingResult.ErrorMessage ?? "Naming fix execution failed.");
                return LauncherRuleFixExecutionResult.Failed(namingResult.ErrorMessage ?? "Naming fix execution failed.");
            }

            namesRenamed = namingResult.NamesRenamed;
        }

        LauncherFixExecutionResult? parameterResult = null;
        if (parameterRequests.Length > 0)
        {
            parameterResult = _parameterFixExecutor.Execute(
                request.Document,
                fixBuildResult.SharedParameterFilePath,
                parameterRequests,
                request.ExecutionLog,
                request.ProgressScope);

            if (!parameterResult.Succeeded)
            {
                ExecutionLogSupport.WriteFixFailed(request.ExecutionLog, parameterResult.ErrorMessage ?? "Fix execution failed.");
                return LauncherRuleFixExecutionResult.Failed(parameterResult.ErrorMessage ?? "Fix execution failed.");
            }
        }

        var parametersAdded = parameterResult?.ParametersAdded ?? 0;
        var valuesAssigned = parameterResult?.ValuesAssigned ?? 0;
        var affectedFamilies = parameterResult?.AffectedFamilies ?? 0;
        var affectedTypes = parameterResult?.AffectedTypes ?? 0;
        var affectedInstances = parameterResult?.AffectedInstances ?? 0;
        var defaultValuesApplied = parameterResult?.DefaultValuesApplied ?? fixBuildResult.CorrectionSummary?.DefaultValuesApplied ?? [];

        ExecutionLogSupport.WriteFixCompleted(
            request.ExecutionLog,
            parametersAdded,
            valuesAssigned,
            affectedFamilies);
        request.ExecutionLog?.WriteInformation("revit-launcher", "Automatic correction finished in Revit.");

        var executedAt = request.ExecutedAt ?? DateTimeOffset.UtcNow;
        var correlationId = request.CorrelationId ?? $"corr-fix-{Guid.NewGuid():N}";
        var reportDirectory = LauncherPathResolver.ResolveReportDirectory(correlationId);
        var correctionProfile = new CorrectionReportProfile();
        var correctionOutput = correctionProfile.Prepare(new CorrectionReportRequest
        {
            RuleId = rule.Metadata.RuleId,
            RuleName = rule.Metadata.Name,
            ParametersAdded = parametersAdded,
            ValuesAssigned = valuesAssigned,
            NamesRenamed = namesRenamed,
            AffectedTypes = affectedTypes,
            AffectedFamilies = affectedFamilies,
            AffectedInstances = affectedInstances,
            DefaultValuesApplied = defaultValuesApplied,
            GeneratedAt = executedAt,
            CorrelationId = correlationId
        });

        var correctionHtml = new HtmlReportRenderer().Render(correctionOutput);
        var correctionJson = new JsonReportRenderer().Render(correctionOutput);
        var correctionPaths = _reportWriter.WriteCorrection(
            reportDirectory,
            rule.Metadata.RuleId,
            correctionHtml,
            correctionJson);

        return new LauncherRuleFixExecutionResult
        {
            Status = LauncherRuleFixExecutionStatus.Completed,
            FixExecutionResult = new LauncherFixExecutionResult
            {
                Succeeded = true,
                ParametersAdded = parametersAdded,
                ValuesAssigned = valuesAssigned,
                AffectedTypes = affectedTypes,
                AffectedFamilies = affectedFamilies,
                AffectedInstances = affectedInstances,
                NamesRenamed = namesRenamed,
                DefaultValuesApplied = defaultValuesApplied,
                ExecutedRequests = parameterResult?.ExecutedRequests ?? []
            },
            CorrectionHtmlReportPath = correctionPaths.HtmlReportPath,
            CorrectionJsonReportPath = correctionPaths.JsonReportPath,
            ReportDirectory = reportDirectory
        };
    }
}

public sealed record LauncherRuleFixExecutionRequest
{
    public required Autodesk.Revit.DB.Document Document { get; init; }

    public required string RuleFilePath { get; init; }

    public required ValidationPipelineResult ValidationResult { get; init; }

    public required BIMCapabilities.Contracts.Adapters.Revit.Read.IFamilyProvider FamilyProvider { get; init; }

    public ExecutionScope? Scope { get; init; }

    public ExecutionEnvironment? Environment { get; init; }

    public string? SharedParameterFilePathOverride { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public bool UserApprovedModification { get; init; }

    public CorrectionProgressScope? ProgressScope { get; init; }

    public IExecutionLog? ExecutionLog { get; init; }
}

public sealed record LauncherRuleFixExecutionResult
{
    public required LauncherRuleFixExecutionStatus Status { get; init; }

    public LauncherFixExecutionResult? FixExecutionResult { get; init; }

    public string? CorrectionHtmlReportPath { get; init; }

    public string? CorrectionJsonReportPath { get; init; }

    public string? HtmlReportPath { get; init; }

    public string? JsonReportPath { get; init; }

    public string? ReportDirectory { get; init; }

    public string? ErrorMessage { get; init; }

    public bool Succeeded => Status == LauncherRuleFixExecutionStatus.Completed;

    public static LauncherRuleFixExecutionResult Failed(string message)
    {
        return new LauncherRuleFixExecutionResult
        {
            Status = LauncherRuleFixExecutionStatus.Failed,
            ErrorMessage = message
        };
    }
}

public enum LauncherRuleFixExecutionStatus
{
    Completed,
    Failed
}
