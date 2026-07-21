using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Parameter;
using SharedParameterContracts = BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

namespace BIMCapabilities.Engines.Parameter.Atoms.SharedParameter;

internal static class SharedParameterValidationAtomSupport
{
    internal const string DefaultExecutedAt = "2026-06-20T09:30:00+00:00";

    internal sealed record ValidationObject(
        string Id,
        string Kind,
        string? Name,
        Dictionary<string, NormalizedParameter> Parameters);

    internal static SharedParameterContracts.SharedParameterValidationResult CreateResult(
        string atomId,
        SharedParameterContracts.SharedParameterValidationRequest request,
        IReadOnlyList<SharedParameterContracts.SharedParameterDefinition> loadedDefinitions,
        IReadOnlyList<SharedParameterContracts.SharedParameterValidationFinding> findings,
        IReadOnlyList<EvidenceRecord> evidence)
    {
        var orderedFindings = findings
            .OrderBy(finding => finding.ObjectId, StringComparer.Ordinal)
            .ThenBy(finding => finding.ParameterName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SharedParameterContracts.SharedParameterValidationResult
        {
            AtomId = atomId,
            LoadedDefinitions = loadedDefinitions,
            Findings = orderedFindings,
            Evidence = evidence,
            Statistics = BuildStatistics(orderedFindings),
            Diagnostics = BuildDiagnostics(atomId, orderedFindings, request.SharedParameterFile.FilePath),
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<SharedParameterContracts.SharedParameterDefinition> ResolveDefinitions(
        SharedParameterContracts.SharedParameterValidationRequest request)
    {
        var loadedDefinitions = SharedParameterDefinitionLoader.Load(request.SharedParameterFile.FilePath);

        if (request.ParameterNamesToValidate is not { Count: > 0 })
        {
            return loadedDefinitions;
        }

        var requestedNames = new HashSet<string>(request.ParameterNamesToValidate, StringComparer.OrdinalIgnoreCase);

        return loadedDefinitions
            .Where(definition => requestedNames.Contains(definition.Name))
            .ToArray();
    }

    internal static IReadOnlyList<SharedParameterContracts.SharedParameterValidationFinding> AnalyzeSharedParameters(
        SharedParameterContracts.SharedParameterValidationRequest request,
        IReadOnlyList<SharedParameterContracts.SharedParameterDefinition> definitions)
    {
        var objects = BuildValidationObjects(request);
        var findings = new List<SharedParameterContracts.SharedParameterValidationFinding>();

        foreach (var validationObject in objects)
        {
            foreach (var definition in definitions)
            {
                validationObject.Parameters.TryGetValue(definition.Name, out var retrievedParameter);
                var status = EvaluateStatus(definition, retrievedParameter);

                findings.Add(new SharedParameterContracts.SharedParameterValidationFinding
                {
                    ObjectId = validationObject.Id,
                    ObjectKind = validationObject.Kind,
                    ObjectName = validationObject.Name,
                    ParameterName = definition.Name,
                    Status = status,
                    Passed = status == SharedParameterContracts.SharedParameterValidationStatus.Valid,
                    ExpectedDefinition = definition,
                    RetrievedParameter = retrievedParameter
                });
            }
        }

        return findings;
    }

    internal static SharedParameterContracts.SharedParameterValidationStatus EvaluateStatus(
        SharedParameterContracts.SharedParameterDefinition expectedDefinition,
        NormalizedParameter? retrievedParameter)
    {
        if (retrievedParameter is null)
        {
            return SharedParameterContracts.SharedParameterValidationStatus.Missing;
        }

        if (!retrievedParameter.IsSharedParameter)
        {
            return SharedParameterContracts.SharedParameterValidationStatus.NotShared;
        }

        return DefinitionMatches(expectedDefinition, retrievedParameter)
            ? SharedParameterContracts.SharedParameterValidationStatus.Valid
            : SharedParameterContracts.SharedParameterValidationStatus.DefinitionMismatch;
    }

    internal static bool DefinitionMatches(
        SharedParameterContracts.SharedParameterDefinition expectedDefinition,
        NormalizedParameter retrievedParameter)
    {
        if (!string.Equals(expectedDefinition.Name, retrievedParameter.Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(expectedDefinition.Guid)
            && !string.Equals(expectedDefinition.Guid, ResolveParameterGuid(retrievedParameter), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(expectedDefinition.DataType)
            && !string.Equals(
                NormalizeDataType(expectedDefinition.DataType),
                NormalizeDataType(retrievedParameter.StorageType.ToString()),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    internal static IReadOnlyList<EvidenceRecord> BuildEvidence(
        SharedParameterContracts.SharedParameterValidationRequest request,
        string atomId,
        IReadOnlyList<SharedParameterContracts.SharedParameterValidationFinding> findings)
    {
        var executedAt = request.ExecutedAt ?? DateTimeOffset.Parse(DefaultExecutedAt);
        var evidence = new List<EvidenceRecord>();

        foreach (var finding in findings.Where(candidate => !candidate.Passed))
        {
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
                Severity = EvidenceSeverity.Error,
                Message = BuildEvidenceMessage(finding),
                StructuredData = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["objectId"] = finding.ObjectId,
                    ["objectKind"] = finding.ObjectKind ?? "object",
                    ["parameterName"] = finding.ParameterName,
                    ["validationStatus"] = finding.Status.ToString(),
                    ["sharedParameterFilePath"] = request.SharedParameterFile.FilePath,
                    ["expectedGuid"] = finding.ExpectedDefinition?.Guid ?? string.Empty,
                    ["retrievedGuid"] = finding.RetrievedParameter is null
                        ? string.Empty
                        : ResolveParameterGuid(finding.RetrievedParameter) ?? string.Empty
                }
            });
        }

        return evidence
            .OrderBy(record => record.EvidenceId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<ValidationObject> BuildValidationObjects(
        SharedParameterContracts.SharedParameterValidationRequest request)
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

    private static string? ResolveParameterGuid(NormalizedParameter parameter)
    {
        if (parameter.Metadata is not null
            && parameter.Metadata.TryGetValue("sharedParameterGuid", out var guid)
            && !string.IsNullOrWhiteSpace(guid))
        {
            return guid;
        }

        return parameter.Identifier.Id;
    }

    private static string NormalizeDataType(string dataType)
    {
        return dataType.ToUpperInvariant() switch
        {
            "TEXT" => nameof(NormalizedParameterStorageType.String),
            "INTEGER" or "INT" => nameof(NormalizedParameterStorageType.Integer),
            "NUMBER" or "DOUBLE" => nameof(NormalizedParameterStorageType.Double),
            "YESNO" or "BOOL" or "BOOLEAN" => nameof(NormalizedParameterStorageType.Boolean),
            _ => dataType
        };
    }

    private static SharedParameterContracts.SharedParameterValidationStatistics BuildStatistics(
        IReadOnlyList<SharedParameterContracts.SharedParameterValidationFinding> findings)
    {
        var objectsChecked = findings
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var missingSharedParameters = findings.Count(finding =>
            finding.Status == SharedParameterContracts.SharedParameterValidationStatus.Missing);
        var invalidSharedParameters = findings.Count(finding =>
            finding.Status is SharedParameterContracts.SharedParameterValidationStatus.NotShared
                or SharedParameterContracts.SharedParameterValidationStatus.DefinitionMismatch);
        var objectsFailed = findings
            .Where(finding => !finding.Passed)
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return new SharedParameterContracts.SharedParameterValidationStatistics
        {
            ObjectsChecked = objectsChecked,
            ObjectsPassed = objectsChecked - objectsFailed,
            ObjectsFailed = objectsFailed,
            SharedParametersChecked = findings.Count,
            MissingSharedParameters = missingSharedParameters,
            InvalidSharedParameters = invalidSharedParameters
        };
    }

    private static IReadOnlyList<ParameterEngineDiagnostic> BuildDiagnostics(
        string atomId,
        IReadOnlyList<SharedParameterContracts.SharedParameterValidationFinding> findings,
        string sharedParameterFilePath)
    {
        var missingSharedParameters = findings.Count(finding =>
            finding.Status == SharedParameterContracts.SharedParameterValidationStatus.Missing);
        var invalidSharedParameters = findings.Count(finding => !finding.Passed) - missingSharedParameters;

        return
        [
            new ParameterEngineDiagnostic
            {
                Code = "SharedParameterValidation.Completed",
                Message = $"Shared parameter atom '{atomId}' validated {findings.Count} shared parameters using '{sharedParameterFilePath}', found {missingSharedParameters} missing and {invalidSharedParameters} invalid shared parameters.",
                Severity = ParameterEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        ];
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        SharedParameterContracts.SharedParameterValidationRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["validationOperation"] = "shared-parameter-validation",
            ["targetSetId"] = request.TargetSet.TargetSetId,
            ["sharedParameterFilePath"] = request.SharedParameterFile.FilePath
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

    private static string BuildEvidenceMessage(SharedParameterContracts.SharedParameterValidationFinding finding)
    {
        return finding.Status switch
        {
            SharedParameterContracts.SharedParameterValidationStatus.Missing =>
                $"Required shared parameter '{finding.ParameterName}' is missing on '{finding.ObjectName ?? finding.ObjectId}'.",
            SharedParameterContracts.SharedParameterValidationStatus.NotShared =>
                $"Parameter '{finding.ParameterName}' on '{finding.ObjectName ?? finding.ObjectId}' is not a shared parameter.",
            SharedParameterContracts.SharedParameterValidationStatus.DefinitionMismatch =>
                $"Shared parameter '{finding.ParameterName}' on '{finding.ObjectName ?? finding.ObjectId}' does not match the expected shared parameter definition.",
            _ => $"Shared parameter '{finding.ParameterName}' validation failed on '{finding.ObjectName ?? finding.ObjectId}'."
        };
    }

    private static string BuildEvidenceId(
        string objectId,
        string parameterName,
        SharedParameterContracts.SharedParameterValidationStatus status)
    {
        return $"shared-parameter-{status}-{objectId}-{parameterName}".ToLowerInvariant();
    }
}
