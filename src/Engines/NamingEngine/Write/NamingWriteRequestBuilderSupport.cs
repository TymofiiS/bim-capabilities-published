using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Engines.Naming.Write;

internal static class NamingWriteRequestBuilderSupport
{
    internal static IReadOnlyList<ResolvedRenameAction> ResolveRenameActions(
        NamingWriteRequestBuildRequest request)
    {
        var findings = request.ComplianceResult.Findings ?? [];
        var correctionIntents = request.CorrectionIntents ?? [];
        var requiredPrefix = ResolveRequiredPrefix(request);
        var resolvedActions = new Dictionary<string, ResolvedRenameAction>(StringComparer.Ordinal);

        foreach (var finding in findings.OrderBy(f => f.ValidationStage, StringComparer.Ordinal)
                     .ThenBy(f => f.ObjectId, StringComparer.Ordinal)
                     .ThenBy(f => f.ObjectName, StringComparer.OrdinalIgnoreCase))
        {
            if (finding.Passed || resolvedActions.ContainsKey(finding.ObjectId))
            {
                continue;
            }

            if (!IsRenameAllowed(finding, request.PrefixFixScope))
            {
                continue;
            }

            var intent = FindCorrectionIntent(correctionIntents, finding.ObjectId);
            var action = ResolveAction(finding, intent);
            if (action is null || string.IsNullOrWhiteSpace(finding.ObjectName))
            {
                continue;
            }

            var proposedName = ResolveProposedName(
                finding.ObjectName,
                requiredPrefix,
                intent);

            resolvedActions[finding.ObjectId] = new ResolvedRenameAction(
                finding,
                action.Value,
                proposedName,
                requiredPrefix,
                intent);
        }

        return resolvedActions.Values
            .OrderBy(action => action.Finding.ObjectId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static WriteRequest CreateWriteRequest(
        NamingWriteRequestBuildRequest request,
        ResolvedRenameAction resolvedAction,
        int order)
    {
        var finding = resolvedAction.Finding;
        var namingRule = ResolveNamingRuleLabel(request, resolvedAction.RequiredPrefix);

        var payload = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["currentName"] = finding.ObjectName!,
            ["proposedName"] = resolvedAction.ProposedName,
            ["namingRule"] = namingRule,
            ["requestedAction"] = resolvedAction.Action.ToString(),
            ["validationStage"] = finding.ValidationStage,
            ["findingStatus"] = finding.Status ?? string.Empty
        };

        return new WriteRequest
        {
            RequestId = CreateRequestId(resolvedAction.Action, finding.ObjectId, finding.ObjectName!),
            TargetObject = ResolveTargetObject(request.TargetSet, finding),
            RequestType = resolvedAction.Action,
            Order = order,
            Payload = payload,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["builderId"] = NamingWriteRequestBuilder.BuilderId,
                ["sourceEngine"] = request.ComplianceResult.EngineId,
                ["currentName"] = finding.ObjectName!,
                ["proposedName"] = resolvedAction.ProposedName,
                ["namingRule"] = namingRule,
                ["validationStage"] = finding.ValidationStage
            },
            CorrelationId = request.CorrelationId,
            RuleId = request.RuleId,
            RequestedAt = request.RequestedAt
        };
    }

    internal static NamingWriteRequestBuildStatistics BuildStatistics(
        int findingsProcessed,
        IReadOnlyList<WriteRequest> writeRequests,
        int skippedFindings)
    {
        return new NamingWriteRequestBuildStatistics
        {
            FindingsProcessed = findingsProcessed,
            RequestsGenerated = writeRequests.Count,
            RenameFamilyRequests = writeRequests.Count(request => request.RequestType == WriteRequestType.RenameFamily),
            RenameTypeRequests = writeRequests.Count(request => request.RequestType == WriteRequestType.RenameType),
            SkippedFindings = skippedFindings
        };
    }

    private static bool IsRenameAllowed(NamingComplianceFinding finding, PrefixFixScope prefixFixScope)
    {
        if (prefixFixScope == PrefixFixScope.None)
        {
            return false;
        }

        return finding.ObjectKind switch
        {
            "familyType" => prefixFixScope.HasFlag(PrefixFixScope.Type),
            "family" => prefixFixScope.HasFlag(PrefixFixScope.Family),
            _ => prefixFixScope.HasFlag(PrefixFixScope.Family)
        };
    }

    private static WriteRequestType? ResolveAction(
        NamingComplianceFinding finding,
        NamingWriteCorrectionIntent? intent)
    {
        if (!string.IsNullOrWhiteSpace(intent?.RequestedAction)
            && Enum.TryParse<WriteRequestType>(intent.RequestedAction, ignoreCase: true, out var requestedAction))
        {
            return requestedAction;
        }

        return finding.ObjectKind switch
        {
            "familyType" => WriteRequestType.RenameType,
            "family" => WriteRequestType.RenameFamily,
            _ => WriteRequestType.RenameFamily
        };
    }

    private static NamingWriteCorrectionIntent? FindCorrectionIntent(
        IReadOnlyList<NamingWriteCorrectionIntent> intents,
        string objectId)
    {
        return intents.LastOrDefault(intent =>
            string.Equals(intent.ObjectId, objectId, StringComparison.Ordinal));
    }

    private static string ResolveRequiredPrefix(NamingWriteRequestBuildRequest request)
    {
        return request.RequiredPrefixes?.FirstOrDefault(prefix => !string.IsNullOrWhiteSpace(prefix))
            ?? request.PatternRule?.TokenizedPattern?.Split('{')[0]
            ?? "DR_";
    }

    private static string ResolveNamingRuleLabel(
        NamingWriteRequestBuildRequest request,
        string requiredPrefix)
    {
        if (!string.IsNullOrWhiteSpace(request.PatternRule?.TokenizedPattern))
        {
            return request.PatternRule.TokenizedPattern;
        }

        return requiredPrefix;
    }

    internal static string ResolveProposedName(
        string currentName,
        string requiredPrefix,
        NamingWriteCorrectionIntent? intent)
    {
        if (!string.IsNullOrWhiteSpace(intent?.ProposedName))
        {
            return intent.ProposedName;
        }

        if (currentName.StartsWith(requiredPrefix, StringComparison.Ordinal))
        {
            var token = currentName[requiredPrefix.Length..]
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);

            return string.IsNullOrWhiteSpace(token)
                ? requiredPrefix.TrimEnd('_')
                : requiredPrefix + token;
        }

        if (currentName.StartsWith("Door_", StringComparison.OrdinalIgnoreCase))
        {
            return requiredPrefix + currentName["Door_".Length..];
        }

        if (currentName.StartsWith("Window_", StringComparison.OrdinalIgnoreCase))
        {
            return requiredPrefix + currentName["Window_".Length..];
        }

        var sanitized = currentName
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);

        return requiredPrefix + sanitized;
    }

    private static NormalizedIdentifier ResolveTargetObject(
        NamingTargetSet targetSet,
        NamingComplianceFinding finding)
    {
        var family = targetSet.TargetFamilies?
            .FirstOrDefault(candidate => string.Equals(candidate.Identity.Id, finding.ObjectId, StringComparison.Ordinal));

        if (family is not null)
        {
            return family.Identity;
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

    private static string CreateRequestId(
        WriteRequestType action,
        string objectId,
        string currentName)
    {
        var actionToken = action == WriteRequestType.RenameType ? "rename-type" : "rename-family";
        return $"naming-write-{actionToken}-{objectId}-{currentName}".ToLowerInvariant();
    }

    internal sealed record ResolvedRenameAction(
        NamingComplianceFinding Finding,
        WriteRequestType Action,
        string ProposedName,
        string RequiredPrefix,
        NamingWriteCorrectionIntent? Intent);
}
