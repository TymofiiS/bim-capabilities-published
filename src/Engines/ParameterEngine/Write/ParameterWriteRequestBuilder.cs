using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Engines.Parameter.Write;

/// <summary>
/// Converts parameter compliance findings into deterministic write requests.
/// </summary>
public sealed class ParameterWriteRequestBuilder : IParameterWriteRequestBuilder
{
    public const string BuilderId = "parameter.write-request-builder";

    string IParameterWriteRequestBuilder.BuilderId => BuilderId;

    public ParameterWriteRequestBuildResult Build(ParameterWriteRequestBuildRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.ComplianceResult);
        ArgumentGuard.ThrowIfNull(request.TargetSet);

        var findings = request.ComplianceResult.Findings ?? [];
        var failedFindings = findings.Where(finding => !finding.Passed).ToArray();
        var resolvedActions = ParameterWriteRequestBuilderSupport.ResolveWriteActions(request);
        var writeRequests = new List<WriteRequest>(resolvedActions.Count);
        var diagnostics = new List<ParameterWriteRequestBuildDiagnostic>
        {
            new()
            {
                Code = "ParameterWriteRequestBuilder.Started",
                Message = "Parameter write request generation started.",
                Severity = ParameterWriteRequestBuildDiagnosticSeverity.Information
            }
        };

        var order = 1;
        foreach (var resolvedAction in resolvedActions)
        {
            writeRequests.Add(ParameterWriteRequestBuilderSupport.CreateWriteRequest(
                request,
                resolvedAction,
                order++));

            diagnostics.Add(new ParameterWriteRequestBuildDiagnostic
            {
                Code = "ParameterWriteRequestBuilder.RequestGenerated",
                Message = $"Generated {resolvedAction.Action} request for parameter '{resolvedAction.Finding.ParameterName}'.",
                Severity = ParameterWriteRequestBuildDiagnosticSeverity.Information,
                ParameterName = resolvedAction.Finding.ParameterName,
                ObjectId = resolvedAction.Finding.ObjectId,
                Data = new Dictionary<string, string>
                {
                    ["requestedAction"] = resolvedAction.Action.ToString(),
                    ["validationStage"] = resolvedAction.Finding.ValidationStage
                }
            });
        }

        var skippedFindings = failedFindings.Length - resolvedActions.Count;
        if (skippedFindings > 0)
        {
            diagnostics.Add(new ParameterWriteRequestBuildDiagnostic
            {
                Code = "ParameterWriteRequestBuilder.FindingsSkipped",
                Message = $"{skippedFindings} failed findings did not map to a supported write action.",
                Severity = ParameterWriteRequestBuildDiagnosticSeverity.Warning,
                Data = new Dictionary<string, string>
                {
                    ["skippedFindings"] = skippedFindings.ToString()
                }
            });
        }

        diagnostics.Add(new ParameterWriteRequestBuildDiagnostic
        {
            Code = "ParameterWriteRequestBuilder.Completed",
            Message = $"Parameter write request generation completed with {writeRequests.Count} requests.",
            Severity = ParameterWriteRequestBuildDiagnosticSeverity.Information,
            Data = new Dictionary<string, string>
            {
                ["requestsGenerated"] = writeRequests.Count.ToString()
            }
        });

        return new ParameterWriteRequestBuildResult
        {
            BuilderId = BuilderId,
            WriteRequests = writeRequests,
            Diagnostics = diagnostics,
            Statistics = ParameterWriteRequestBuilderSupport.BuildStatistics(
                findings.Count,
                writeRequests,
                skippedFindings),
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["builderOperation"] = "parameter-write-request-build",
                ["targetSetId"] = request.TargetSet.TargetSetId,
                ["correlationId"] = request.CorrelationId ?? string.Empty,
                ["ruleId"] = request.RuleId ?? string.Empty
            }
        };
    }
}
