using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Engines.Parameter.Write;

internal static class ParameterWriteRequestBuilderSupport
{
    internal static readonly IReadOnlyDictionary<string, string> MvpDefaultValues =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["FireRating"] = "60 min",
            ["RoomName"] = "Lobby",
            ["AcousticRating"] = "45 dB",
            ["Manufacturer"] = "HTL Components"
        };

    internal static IReadOnlyList<ResolvedWriteAction> ResolveWriteActions(
        ParameterWriteRequestBuildRequest request)
    {
        var findings = request.ComplianceResult.Findings ?? [];
        var correctionIntents = request.CorrectionIntents ?? [];
        var resolvedActions = new Dictionary<(string ObjectId, string ParameterName), ResolvedWriteAction>();

        foreach (var finding in findings.OrderBy(f => f.ValidationStage, StringComparer.Ordinal)
                     .ThenBy(f => f.ObjectId, StringComparer.Ordinal)
                     .ThenBy(f => f.ParameterName, StringComparer.OrdinalIgnoreCase))
        {
            if (finding.Passed)
            {
                continue;
            }

            var key = (finding.ObjectId, finding.ParameterName);
            if (resolvedActions.ContainsKey(key))
            {
                continue;
            }

            var intent = FindCorrectionIntent(correctionIntents, finding.ObjectId, finding.ParameterName);
            var action = ResolveAction(finding, intent);
            if (action is null)
            {
                continue;
            }

            resolvedActions[key] = new ResolvedWriteAction(finding, action.Value, intent);
        }

        foreach (var intent in correctionIntents)
        {
            if (intent.RequestedAction != WriteRequestType.ParameterDelete || string.IsNullOrWhiteSpace(intent.ObjectId))
            {
                continue;
            }

            var key = (intent.ObjectId, intent.ParameterName);
            if (resolvedActions.ContainsKey(key))
            {
                continue;
            }

            var syntheticFinding = new ParameterComplianceFinding
            {
                ValidationStage = "correction-intent",
                ObjectId = intent.ObjectId,
                ObjectKind = ResolveObjectKind(request.TargetSet, intent.ObjectId),
                ParameterName = intent.ParameterName,
                Passed = false,
                Status = "DeleteRequested",
                Message = $"Delete requested for parameter '{intent.ParameterName}'."
            };

            resolvedActions[key] = new ResolvedWriteAction(
                syntheticFinding,
                WriteRequestType.ParameterDelete,
                intent);
        }

        return resolvedActions.Values
            .OrderBy(action => action.Finding.ValidationStage, StringComparer.Ordinal)
            .ThenBy(action => action.Finding.ObjectId, StringComparer.Ordinal)
            .ThenBy(action => action.Finding.ParameterName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static WriteRequest CreateWriteRequest(
        ParameterWriteRequestBuildRequest request,
        ResolvedWriteAction resolvedAction,
        int order)
    {
        var finding = resolvedAction.Finding;
        var definition = FindSharedParameterDefinition(
            request.SharedParameterDefinitions,
            finding.ParameterName);
        var requestedValue = ResolveRequestedValue(
            resolvedAction.Finding,
            resolvedAction.Intent,
            request.ParameterDefaults,
            request.ParameterFillRules,
            request.TargetSet);

        var payload = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["parameterName"] = finding.ParameterName,
            ["requestedAction"] = resolvedAction.Action.ToString(),
            ["validationStage"] = finding.ValidationStage,
            ["findingStatus"] = finding.Status ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(requestedValue))
        {
            payload["requestedValue"] = requestedValue;
        }

        if (!string.IsNullOrWhiteSpace(definition?.Guid))
        {
            payload["parameterDefinitionGuid"] = definition.Guid;
        }

        if (!string.IsNullOrWhiteSpace(definition?.DataType))
        {
            payload["parameterDataType"] = definition.DataType;
        }

        if (!string.IsNullOrWhiteSpace(definition?.Group))
        {
            payload["parameterGroup"] = definition.Group;
        }

        payload["parameterIsInstance"] = ResolveParameterIsInstance(finding.ParameterName, request.ParameterBindings)
            ? "true"
            : "false";

        return new WriteRequest
        {
            RequestId = CreateRequestId(resolvedAction.Action, finding.ObjectId, finding.ParameterName),
            TargetObject = ResolveTargetObject(request.TargetSet, finding),
            RequestType = resolvedAction.Action,
            Order = order,
            Payload = payload,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["builderId"] = ParameterWriteRequestBuilder.BuilderId,
                ["sourceEngine"] = request.ComplianceResult.EngineId,
                ["parameterName"] = finding.ParameterName,
                ["validationStage"] = finding.ValidationStage
            },
            CorrelationId = request.CorrelationId,
            RuleId = request.RuleId,
            RequestedAt = request.RequestedAt
        };
    }

    internal static ParameterWriteRequestBuildStatistics BuildStatistics(
        int findingsProcessed,
        IReadOnlyList<WriteRequest> writeRequests,
        int skippedFindings)
    {
        return new ParameterWriteRequestBuildStatistics
        {
            FindingsProcessed = findingsProcessed,
            RequestsGenerated = writeRequests.Count,
            CreateRequests = writeRequests.Count(request => request.RequestType == WriteRequestType.ParameterCreate),
            UpdateRequests = writeRequests.Count(request => request.RequestType == WriteRequestType.ParameterUpdate),
            DeleteRequests = writeRequests.Count(request => request.RequestType == WriteRequestType.ParameterDelete),
            SkippedFindings = skippedFindings
        };
    }

    private static WriteRequestType? ResolveAction(
        ParameterComplianceFinding finding,
        ParameterWriteCorrectionIntent? intent)
    {
        if (intent?.RequestedAction is not null)
        {
            return intent.RequestedAction;
        }

        return finding.ValidationStage switch
        {
            "existence" when finding.Status == "Missing" => WriteRequestType.ParameterCreate,
            "shared-parameter" when finding.Status is "Missing" or "NotShared" => WriteRequestType.ParameterCreate,
            "shared-parameter" when finding.Status == "DefinitionMismatch" => WriteRequestType.ParameterUpdate,
            "value" when finding.Status is "MissingValue" or "InvalidValue" => WriteRequestType.ParameterUpdate,
            _ => null
        };
    }

    private static ParameterWriteCorrectionIntent? FindCorrectionIntent(
        IReadOnlyList<ParameterWriteCorrectionIntent> intents,
        string objectId,
        string parameterName)
    {
        return intents.LastOrDefault(intent =>
            string.Equals(intent.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase)
            && (intent.ObjectId is null || string.Equals(intent.ObjectId, objectId, StringComparison.Ordinal)));
    }

    private static string? ResolveRequestedValue(
        ParameterComplianceFinding finding,
        ParameterWriteCorrectionIntent? intent,
        IReadOnlyDictionary<string, string>? parameterDefaults,
        IReadOnlyDictionary<string, string>? parameterFillRules,
        ParameterTargetSet targetSet)
    {
        if (!string.IsNullOrWhiteSpace(intent?.RequestedValue))
        {
            return intent.RequestedValue;
        }

        if (parameterFillRules is not null
            && parameterFillRules.TryGetValue(finding.ParameterName, out var fillRule))
        {
            var fillValue = ParameterFillRuleSupport.ResolveFillValue(finding, targetSet, fillRule);
            if (!string.IsNullOrWhiteSpace(fillValue))
            {
                return fillValue;
            }
        }

        if (parameterDefaults is not null
            && parameterDefaults.TryGetValue(finding.ParameterName, out var configuredDefault))
        {
            return configuredDefault;
        }

        return MvpDefaultValues.TryGetValue(finding.ParameterName, out var defaultValue)
            ? defaultValue
            : null;
    }

    private static bool ResolveParameterIsInstance(
        string parameterName,
        IReadOnlyDictionary<string, bool>? parameterBindings)
    {
        if (parameterBindings is not null
            && parameterBindings.TryGetValue(parameterName, out var isInstance))
        {
            return isInstance;
        }

        return false;
    }

    private static SharedParameterDefinition? FindSharedParameterDefinition(
        IReadOnlyList<SharedParameterDefinition>? definitions,
        string parameterName)
    {
        return definitions?.LastOrDefault(definition =>
            string.Equals(definition.Name, parameterName, StringComparison.OrdinalIgnoreCase));
    }

    private static NormalizedIdentifier ResolveTargetObject(
        ParameterTargetSet targetSet,
        ParameterComplianceFinding finding)
    {
        var instance = targetSet.TargetInstances?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));

        if (instance is not null)
        {
            var family = targetSet.TargetFamilies?
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, instance.FamilyName, StringComparison.OrdinalIgnoreCase));

            if (family is not null)
            {
                return family.Identity;
            }
        }

        var familyMatch = targetSet.TargetFamilies?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));

        if (familyMatch is not null)
        {
            return familyMatch.Identity;
        }

        var familyType = targetSet.TargetTypes?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));

        if (familyType is not null)
        {
            return familyType.Identity;
        }

        return new NormalizedIdentifier
        {
            Id = finding.ObjectId,
            Kind = finding.ObjectKind ?? "family",
            Scope = "project-document"
        };
    }

    private static string? ResolveObjectKind(ParameterTargetSet targetSet, string objectId)
    {
        if (targetSet.TargetFamilies?.Any(family => family.Identity.Id == objectId) == true)
        {
            return "family";
        }

        if (targetSet.TargetTypes?.Any(type => type.Identity.Id == objectId) == true)
        {
            return "familyType";
        }

        return null;
    }

    private static string CreateRequestId(
        WriteRequestType action,
        string objectId,
        string parameterName)
    {
        var actionToken = action switch
        {
            WriteRequestType.ParameterCreate => "create",
            WriteRequestType.ParameterUpdate => "update",
            WriteRequestType.ParameterDelete => "delete",
            _ => "custom"
        };

        return $"parameter-write-{actionToken}-{objectId}-{parameterName}".ToLowerInvariant();
    }

    internal sealed record ResolvedWriteAction(
        ParameterComplianceFinding Finding,
        WriteRequestType Action,
        ParameterWriteCorrectionIntent? Intent);
}
