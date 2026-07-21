using System.Text.RegularExpressions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Parameter;
using ValueContracts = BIMCapabilities.Contracts.Engines.Parameter.Value;

namespace BIMCapabilities.Engines.Parameter.Atoms.Value;

internal static class ParameterValueValidationAtomSupport
{
    internal const string DefaultExecutedAt = "2026-06-20T10:00:00+00:00";

    internal sealed record ValidationObject(
        string Id,
        string Kind,
        string? Name,
        Dictionary<string, NormalizedParameter> Parameters,
        IReadOnlyDictionary<string, string>? Context = null);

    internal static ValueContracts.ParameterValueValidationResult CreateResult(
        string atomId,
        ValueContracts.ParameterValueValidationRequest request,
        IReadOnlyList<ValueContracts.ParameterValueValidationFinding> findings,
        IReadOnlyList<EvidenceRecord> evidence)
    {
        var orderedFindings = findings
            .OrderBy(finding => finding.ObjectId, StringComparer.Ordinal)
            .ThenBy(finding => finding.ParameterName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ValueContracts.ParameterValueValidationResult
        {
            AtomId = atomId,
            Findings = orderedFindings,
            Evidence = evidence,
            Statistics = BuildStatistics(orderedFindings),
            Diagnostics = BuildDiagnostics(atomId, orderedFindings),
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<ValueContracts.ParameterValueValidationFinding> AnalyzeValues(
        ValueContracts.ParameterValueValidationRequest request)
    {
        var bindings = request.ParameterBindings ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var rules = (request.Rules ?? [])
            .OrderBy(rule => rule.ParameterName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var typeRules = rules
            .Where(rule => !IsInstanceBinding(bindings, rule.ParameterName))
            .ToArray();
        var instanceRules = rules
            .Where(rule => IsInstanceBinding(bindings, rule.ParameterName))
            .ToArray();

        var findings = new List<ValueContracts.ParameterValueValidationFinding>();

        foreach (var validationObject in BuildTypeValidationObjects(request))
        {
            foreach (var rule in typeRules)
            {
                findings.Add(EvaluateObject(validationObject, rule));
            }
        }

        foreach (var validationObject in BuildInstanceValidationObjects(request))
        {
            foreach (var rule in instanceRules)
            {
                if (!validationObject.Parameters.ContainsKey(rule.ParameterName))
                {
                    continue;
                }

                findings.Add(EvaluateObject(validationObject, rule));
            }
        }

        return findings;
    }

    private static ValueContracts.ParameterValueValidationFinding EvaluateObject(
        ValidationObject validationObject,
        ValueContracts.ParameterValueRule rule)
    {
        validationObject.Parameters.TryGetValue(rule.ParameterName, out var retrievedParameter);
        var actualValue = GetNormalizedValue(retrievedParameter);
        var evaluation = EvaluateRule(rule, actualValue);

        return new ValueContracts.ParameterValueValidationFinding
        {
            ObjectId = validationObject.Id,
            ObjectKind = validationObject.Kind,
            ObjectName = validationObject.Name,
            ParameterName = rule.ParameterName,
            Status = evaluation.Status,
            Passed = evaluation.Status == ValueContracts.ParameterValueValidationStatus.Valid,
            ActualValue = actualValue,
            Rule = rule,
            ViolationReason = evaluation.ViolationReason
        };
    }

    private static bool IsInstanceBinding(
        IReadOnlyDictionary<string, bool> bindings,
        string parameterName)
    {
        return bindings.TryGetValue(parameterName, out var isInstance) && isInstance;
    }

    internal static (ValueContracts.ParameterValueValidationStatus Status, string? ViolationReason) EvaluateRule(
        ValueContracts.ParameterValueRule rule,
        string? actualValue)
    {
        var hasValue = !string.IsNullOrWhiteSpace(actualValue);

        if (!hasValue)
        {
            if (rule.RequiredValue || HasValueConstraints(rule))
            {
                return (ValueContracts.ParameterValueValidationStatus.MissingValue, "Parameter value is missing or empty.");
            }

            return (ValueContracts.ParameterValueValidationStatus.Valid, null);
        }

        if (rule.AllowedValues is { Count: > 0 }
            && !rule.AllowedValues.Any(allowedValue =>
                string.Equals(allowedValue, actualValue, StringComparison.OrdinalIgnoreCase)))
        {
            return (ValueContracts.ParameterValueValidationStatus.InvalidValue, "Parameter value is not in the allowed values list.");
        }

        if (rule.ForbiddenValues is { Count: > 0 }
            && rule.ForbiddenValues.Any(forbiddenValue =>
                string.Equals(forbiddenValue, actualValue, StringComparison.OrdinalIgnoreCase)))
        {
            return (ValueContracts.ParameterValueValidationStatus.InvalidValue, "Parameter value is in the forbidden values list.");
        }

        if (rule.MinimumLength is int minimumLength && actualValue!.Length < minimumLength)
        {
            return (ValueContracts.ParameterValueValidationStatus.InvalidValue, $"Parameter value length is less than the minimum length of {minimumLength}.");
        }

        if (rule.MaximumLength is int maximumLength && actualValue!.Length > maximumLength)
        {
            return (ValueContracts.ParameterValueValidationStatus.InvalidValue, $"Parameter value length exceeds the maximum length of {maximumLength}.");
        }

        if (!string.IsNullOrWhiteSpace(rule.RegularExpression)
            && !MatchesRegularExpression(actualValue!, rule.RegularExpression))
        {
            return (ValueContracts.ParameterValueValidationStatus.InvalidValue, "Parameter value does not match the required regular expression.");
        }

        return (ValueContracts.ParameterValueValidationStatus.Valid, null);
    }

    internal static IReadOnlyList<EvidenceRecord> BuildEvidence(
        ValueContracts.ParameterValueValidationRequest request,
        string atomId,
        IReadOnlyList<ValueContracts.ParameterValueValidationFinding> findings)
    {
        var executedAt = request.ExecutedAt ?? DateTimeOffset.Parse(DefaultExecutedAt);
        var evidence = new List<EvidenceRecord>();
        var instanceLookup = (request.TargetSet.TargetInstances ?? [])
            .ToDictionary(instance => instance.Identity.Id, StringComparer.Ordinal);

        foreach (var finding in findings.Where(candidate => !candidate.Passed))
        {
            var severity = finding.Rule?.Severity ?? EvidenceSeverity.Error;
            var structuredData = BuildStructuredData(finding);
            if (finding.ObjectKind == "familyInstance")
            {
                structuredData["validationScope"] = "instance";
                if (instanceLookup.TryGetValue(finding.ObjectId, out var instance))
                {
                    structuredData["familyName"] = instance.FamilyName;
                    structuredData["typeName"] = instance.FamilyTypeName;
                    if (!string.IsNullOrWhiteSpace(instance.CategoryName))
                    {
                        structuredData["categoryName"] = instance.CategoryName;
                    }
                }
            }

            evidence.Add(new EvidenceRecord
            {
                EvidenceId = BuildEvidenceId(finding.ObjectId, finding.ParameterName, finding.Status),
                Timestamp = executedAt,
                Source = new EvidenceSource
                {
                    EngineId = "parameter-engine",
                    AtomId = atomId,
                    RuleId = request.RuleId,
                    CapabilityId = atomId
                },
                Target = new EvidenceTarget
                {
                    TargetType = finding.ObjectKind ?? "object",
                    TargetId = finding.ObjectId,
                    TargetName = finding.ObjectName,
                    TargetSetDescription = request.TargetSet.TargetSetId
                },
                Category = EvidenceCategory.Validation,
                Severity = severity,
                Message = BuildEvidenceMessage(finding),
                StructuredData = structuredData
            });
        }

        return evidence
            .OrderBy(record => record.EvidenceId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<ValidationObject> BuildTypeValidationObjects(
        ValueContracts.ParameterValueValidationRequest request)
    {
        return BuildValidationObjects(request);
    }

    internal static IReadOnlyList<ValidationObject> BuildInstanceValidationObjects(
        ValueContracts.ParameterValueValidationRequest request)
    {
        if (request.TargetSet.TargetInstances is not { Count: > 0 } targetInstances)
        {
            return [];
        }

        return targetInstances
            .OrderBy(instance => instance.Identity.Id, StringComparer.Ordinal)
            .Select(instance =>
            {
                var context = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["familyName"] = instance.FamilyName,
                    ["typeName"] = instance.FamilyTypeName,
                    ["objectKind"] = "familyInstance"
                };

                if (!string.IsNullOrWhiteSpace(instance.CategoryName))
                {
                    context["categoryName"] = instance.CategoryName;
                }

                return new ValidationObject(
                    instance.Identity.Id,
                    "familyInstance",
                    instance.Name ?? instance.Identity.Id,
                    CollectParameters(instance.Parameters),
                    context);
            })
            .ToArray();
    }

    internal static IReadOnlyList<ValidationObject> BuildValidationObjects(
        ValueContracts.ParameterValueValidationRequest request)
    {
        var targetSet = request.TargetSet;
        var setLevelParameters = CollectParameters(targetSet.TargetParameters);
        var queryParameters = CollectParameters(request.ParameterQueryResult?.Parameters);

        if (targetSet.TargetTypes is { Count: > 0 })
        {
            return targetSet.TargetTypes
                .OrderBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
                .Select(familyType =>
                {
                    var parameters = MergeParameters(setLevelParameters, CollectParameters(familyType.Parameters), queryParameters);
                    return new ValidationObject(
                        familyType.Identity.Id,
                        "familyType",
                        familyType.Name,
                        parameters);
                })
                .ToArray();
        }

        if (targetSet.TargetFamilies is { Count: > 0 })
        {
            return targetSet.TargetFamilies
                .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
                .Select(family =>
                {
                    var parameters = MergeParameters(
                        setLevelParameters,
                        CollectParameters(family.Parameters),
                        CollectParametersFromFamilyTypes(family.FamilyTypes),
                        queryParameters);
                    return new ValidationObject(
                        family.Identity.Id,
                        "family",
                        family.Name,
                        parameters);
                })
                .ToArray();
        }

        var fallbackParameters = MergeParameters(setLevelParameters, queryParameters);

        return
        [
            new ValidationObject(
                targetSet.TargetSetId,
                "targetSet",
                targetSet.TargetSetId,
                fallbackParameters)
        ];
    }

    private static bool HasValueConstraints(ValueContracts.ParameterValueRule rule)
    {
        return rule.AllowedValues is { Count: > 0 }
            || rule.ForbiddenValues is { Count: > 0 }
            || rule.MinimumLength is not null
            || rule.MaximumLength is not null
            || !string.IsNullOrWhiteSpace(rule.RegularExpression);
    }

    private static bool MatchesRegularExpression(string value, string pattern)
    {
        return Regex.IsMatch(
            value,
            pattern,
            RegexValidationOptions.Default,
            TimeSpan.FromSeconds(1));
    }

    private static Dictionary<string, NormalizedParameter> CollectParametersFromFamilyTypes(
        IReadOnlyList<NormalizedFamilyType>? familyTypes)
    {
        var parameters = new Dictionary<string, NormalizedParameter>(StringComparer.OrdinalIgnoreCase);

        if (familyTypes is null)
        {
            return parameters;
        }

        foreach (var familyType in familyTypes)
        {
            foreach (var pair in CollectParameters(familyType.Parameters))
            {
                parameters[pair.Key] = pair.Value;
            }
        }

        return parameters;
    }

    private static Dictionary<string, NormalizedParameter> CollectParameters(
        IReadOnlyList<NormalizedParameter>? parameters)
    {
        var map = new Dictionary<string, NormalizedParameter>(StringComparer.OrdinalIgnoreCase);

        if (parameters is null)
        {
            return map;
        }

        foreach (var parameter in parameters)
        {
            map[parameter.Name] = parameter;
        }

        return map;
    }

    private static Dictionary<string, NormalizedParameter> MergeParameters(
        params Dictionary<string, NormalizedParameter>[] sources)
    {
        var merged = new Dictionary<string, NormalizedParameter>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            foreach (var pair in source)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }

    private static string? GetNormalizedValue(NormalizedParameter? parameter)
    {
        return string.IsNullOrWhiteSpace(parameter?.Value) ? null : parameter.Value.Trim();
    }

    private static ValueContracts.ParameterValueValidationStatistics BuildStatistics(
        IReadOnlyList<ValueContracts.ParameterValueValidationFinding> findings)
    {
        var objectsChecked = findings
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var missingValues = findings.Count(finding =>
            finding.Status == ValueContracts.ParameterValueValidationStatus.MissingValue);
        var invalidValues = findings.Count(finding =>
            finding.Status == ValueContracts.ParameterValueValidationStatus.InvalidValue);
        var objectsFailed = findings
            .Where(finding => !finding.Passed)
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return new ValueContracts.ParameterValueValidationStatistics
        {
            ObjectsChecked = objectsChecked,
            ObjectsPassed = objectsChecked - objectsFailed,
            ObjectsFailed = objectsFailed,
            ParametersChecked = findings.Count,
            MissingValues = missingValues,
            InvalidValues = invalidValues
        };
    }

    private static IReadOnlyList<ParameterEngineDiagnostic> BuildDiagnostics(
        string atomId,
        IReadOnlyList<ValueContracts.ParameterValueValidationFinding> findings)
    {
        var missingValues = findings.Count(finding =>
            finding.Status == ValueContracts.ParameterValueValidationStatus.MissingValue);
        var invalidValues = findings.Count(finding =>
            finding.Status == ValueContracts.ParameterValueValidationStatus.InvalidValue);

        return
        [
            new ParameterEngineDiagnostic
            {
                Code = "ParameterValueValidation.Completed",
                Message = $"Parameter value atom '{atomId}' checked {findings.Count} parameter values, found {missingValues} missing values and {invalidValues} invalid values.",
                Severity = ParameterEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        ];
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        ValueContracts.ParameterValueValidationRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["validationOperation"] = "parameter-value-validation",
            ["targetSetId"] = request.TargetSet.TargetSetId
        };

        if (!string.IsNullOrWhiteSpace(request.RuleId))
        {
            metadata["ruleId"] = request.RuleId;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata["correlationId"] = request.CorrelationId;
        }

        if (request.Metadata is not null)
        {
            foreach (var pair in request.Metadata.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return metadata;
    }

    private static Dictionary<string, string> BuildStructuredData(
        ValueContracts.ParameterValueValidationFinding finding)
    {
        var structuredData = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["objectId"] = finding.ObjectId,
            ["objectKind"] = finding.ObjectKind ?? "object",
            ["parameterName"] = finding.ParameterName,
            ["validationStatus"] = finding.Status.ToString(),
            ["actualValue"] = finding.ActualValue ?? string.Empty,
            ["violationReason"] = finding.ViolationReason ?? string.Empty
        };

        if (finding.ObjectKind == "familyInstance")
        {
            structuredData["validationScope"] = "instance";
        }

        if (!string.IsNullOrWhiteSpace(finding.Rule?.CustomRuleIdentifier))
        {
            structuredData["customRuleIdentifier"] = finding.Rule.CustomRuleIdentifier;
        }

        return structuredData;
    }

    private static string BuildEvidenceMessage(ValueContracts.ParameterValueValidationFinding finding)
    {
        var targetLabel = finding.ObjectName ?? finding.ObjectId;
        if (finding.ObjectKind == "familyInstance")
        {
            targetLabel = $"instance '{targetLabel}'";
        }

        return finding.Status switch
        {
            ValueContracts.ParameterValueValidationStatus.MissingValue =>
                $"Parameter '{finding.ParameterName}' on {targetLabel} is missing a required value.",
            ValueContracts.ParameterValueValidationStatus.InvalidValue =>
                $"Parameter '{finding.ParameterName}' on {targetLabel} has an invalid value '{finding.ActualValue ?? string.Empty}'.",
            _ => $"Parameter '{finding.ParameterName}' validation failed on {targetLabel}."
        };
    }

    private static string BuildEvidenceId(
        string objectId,
        string parameterName,
        ValueContracts.ParameterValueValidationStatus status)
    {
        return $"parameter-value-{status}-{objectId}-{parameterName}".ToLowerInvariant();
    }
}
