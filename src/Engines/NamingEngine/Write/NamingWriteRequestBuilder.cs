using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Engines.Naming.Write;

/// <summary>
/// Converts naming compliance findings into deterministic write requests.
/// </summary>
public sealed class NamingWriteRequestBuilder : INamingWriteRequestBuilder
{
    public const string BuilderId = "naming.write-request-builder";

    string INamingWriteRequestBuilder.BuilderId => BuilderId;

    public NamingWriteRequestBuildResult Build(NamingWriteRequestBuildRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.ComplianceResult);
        ArgumentGuard.ThrowIfNull(request.TargetSet);

        var findings = request.ComplianceResult.Findings ?? [];
        var failedFindings = findings.Where(finding => !finding.Passed).ToArray();
        var resolvedActions = NamingWriteRequestBuilderSupport.ResolveRenameActions(request);
        var writeRequests = new List<WriteRequest>(resolvedActions.Count);
        var diagnostics = new List<NamingWriteRequestBuildDiagnostic>
        {
            new()
            {
                Code = "NamingWriteRequestBuilder.Started",
                Message = "Naming write request generation started.",
                Severity = NamingWriteRequestBuildDiagnosticSeverity.Information
            }
        };

        var order = 1;
        foreach (var resolvedAction in resolvedActions)
        {
            writeRequests.Add(NamingWriteRequestBuilderSupport.CreateWriteRequest(
                request,
                resolvedAction,
                order++));

            diagnostics.Add(new NamingWriteRequestBuildDiagnostic
            {
                Code = "NamingWriteRequestBuilder.RequestGenerated",
                Message = $"Generated {resolvedAction.Action} request for '{resolvedAction.Finding.ObjectName}'.",
                Severity = NamingWriteRequestBuildDiagnosticSeverity.Information,
                ObjectId = resolvedAction.Finding.ObjectId,
                CurrentName = resolvedAction.Finding.ObjectName,
                Data = new Dictionary<string, string>
                {
                    ["requestedAction"] = resolvedAction.Action.ToString(),
                    ["proposedName"] = resolvedAction.ProposedName,
                    ["validationStage"] = resolvedAction.Finding.ValidationStage
                }
            });
        }

        var skippedFindings = failedFindings
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count() - resolvedActions.Count;

        if (skippedFindings > 0)
        {
            diagnostics.Add(new NamingWriteRequestBuildDiagnostic
            {
                Code = "NamingWriteRequestBuilder.FindingsSkipped",
                Message = $"{skippedFindings} failed findings did not map to a supported rename action.",
                Severity = NamingWriteRequestBuildDiagnosticSeverity.Warning,
                Data = new Dictionary<string, string>
                {
                    ["skippedFindings"] = skippedFindings.ToString()
                }
            });
        }

        diagnostics.Add(new NamingWriteRequestBuildDiagnostic
        {
            Code = "NamingWriteRequestBuilder.Completed",
            Message = $"Naming write request generation completed with {writeRequests.Count} requests.",
            Severity = NamingWriteRequestBuildDiagnosticSeverity.Information,
            Data = new Dictionary<string, string>
            {
                ["requestsGenerated"] = writeRequests.Count.ToString()
            }
        });

        return new NamingWriteRequestBuildResult
        {
            BuilderId = BuilderId,
            WriteRequests = writeRequests,
            Diagnostics = diagnostics,
            Statistics = NamingWriteRequestBuilderSupport.BuildStatistics(
                findings.Count,
                writeRequests,
                skippedFindings),
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["builderOperation"] = "naming-write-request-build",
                ["targetSetId"] = request.TargetSet.TargetSetId,
                ["correlationId"] = request.CorrelationId ?? string.Empty,
                ["ruleId"] = request.RuleId ?? string.Empty,
                ["namingRule"] = request.PatternRule?.TokenizedPattern
                    ?? request.RequiredPrefixes?.FirstOrDefault()
                    ?? string.Empty
            }
        };
    }
}
