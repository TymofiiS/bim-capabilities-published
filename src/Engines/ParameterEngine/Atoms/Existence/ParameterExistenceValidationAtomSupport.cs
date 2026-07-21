using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Parameter;
using ExistenceContracts = BIMCapabilities.Contracts.Engines.Parameter.Existence;

namespace BIMCapabilities.Engines.Parameter.Atoms.Existence;

internal static class ParameterExistenceValidationAtomSupport
{
    internal const string DefaultExecutedAt = "2026-06-20T09:00:00+00:00";

    internal sealed record ValidationObject(
        string Id,
        string Kind,
        string? Name,
        string? FamilyName,
        string? CategoryName,
        int PlacedInstanceCount,
        HashSet<string> ParameterNames);

    internal static ExistenceContracts.ParameterExistenceResult CreateResult(
        string atomId,
        ExistenceContracts.ParameterExistenceRequest request,
        IReadOnlyList<ExistenceContracts.ParameterExistenceFinding> findings,
        IReadOnlyList<EvidenceRecord> evidence)
    {
        var orderedFindings = findings
            .OrderBy(finding => finding.ObjectId, StringComparer.Ordinal)
            .ThenBy(finding => finding.ParameterName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ExistenceContracts.ParameterExistenceResult
        {
            AtomId = atomId,
            Findings = orderedFindings,
            Evidence = evidence,
            Statistics = BuildStatistics(orderedFindings),
            Diagnostics = BuildDiagnostics(atomId, orderedFindings),
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<ExistenceContracts.ParameterExistenceFinding> AnalyzeExistence(
        ExistenceContracts.ParameterExistenceRequest request)
    {
        var requiredNames = (request.RequiredParameterNames ?? [])
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var bindings = request.ParameterBindings ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var findings = new List<ExistenceContracts.ParameterExistenceFinding>();

        foreach (var parameterName in requiredNames)
        {
            var validationObjects = IsInstanceBinding(bindings, parameterName)
                ? BuildFamilyValidationObjects(request)
                : BuildTypeValidationObjects(request);

            foreach (var validationObject in validationObjects)
            {
                findings.Add(new ExistenceContracts.ParameterExistenceFinding
                {
                    ObjectId = validationObject.Id,
                    ObjectKind = validationObject.Kind,
                    ObjectName = validationObject.Name,
                    ParameterName = parameterName,
                    Exists = validationObject.ParameterNames.Contains(parameterName)
                });
            }
        }

        return findings;
    }

    private static bool IsInstanceBinding(
        IReadOnlyDictionary<string, bool> bindings,
        string parameterName)
    {
        return bindings.TryGetValue(parameterName, out var isInstance) && isInstance;
    }

    internal static IReadOnlyList<EvidenceRecord> BuildEvidence(
        ExistenceContracts.ParameterExistenceRequest request,
        string atomId,
        IReadOnlyList<ExistenceContracts.ParameterExistenceFinding> findings)
    {
        var executedAt = request.ExecutedAt ?? DateTimeOffset.Parse(DefaultExecutedAt);
        var evidence = new List<EvidenceRecord>();
        var categoryName = ResolveCategoryName(request);
        var objects = BuildAllValidationObjects(request)
            .ToDictionary(obj => obj.Id, StringComparer.Ordinal);

        foreach (var finding in findings.Where(candidate => !candidate.Exists))
        {
            objects.TryGetValue(finding.ObjectId, out var validationObject);
            var displayName = validationObject?.Name ?? finding.ObjectName;
            var familyName = validationObject?.FamilyName;
            var placedInstanceCount = validationObject?.PlacedInstanceCount ?? 0;

            var structuredData = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["objectKind"] = finding.ObjectKind ?? "object",
                ["parameterName"] = finding.ParameterName,
                ["expectedState"] = "Present",
                ["actualState"] = "Missing",
                ["typeName"] = displayName ?? string.Empty,
                ["placedInstanceCount"] = placedInstanceCount.ToString()
            };

            if (!string.IsNullOrWhiteSpace(familyName))
            {
                structuredData["familyName"] = familyName;
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                structuredData["categoryName"] = categoryName;
            }

            evidence.Add(new EvidenceRecord
            {
                EvidenceId = BuildEvidenceId(finding.ObjectId, finding.ParameterName),
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
                    TargetName = displayName,
                    TargetSetDescription = categoryName
                },
                Category = EvidenceCategory.Validation,
                Severity = EvidenceSeverity.Error,
                Message = $"Required parameter '{finding.ParameterName}' is missing on '{displayName ?? finding.ObjectId}'.",
                StructuredData = structuredData
            });
        }

        return evidence
            .OrderBy(record => record.EvidenceId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<ValidationObject> BuildValidationObjects(
        ExistenceContracts.ParameterExistenceRequest request)
    {
        return BuildAllValidationObjects(request);
    }

    internal static IReadOnlyList<ValidationObject> BuildTypeValidationObjects(
        ExistenceContracts.ParameterExistenceRequest request)
    {
        var targetSet = request.TargetSet;
        var setLevelNames = CollectParameterNames(targetSet.TargetParameters);
        var queryNames = CollectParameterNames(request.ParameterQueryResult?.Parameters);
        var typeContext = BuildTypeContext(targetSet.TargetFamilies);
        var familyParameterNamesByTypeId = BuildFamilyParameterNamesByTypeId(targetSet.TargetFamilies);
        var categoryName = ResolveCategoryName(request);

        if (targetSet.TargetTypes is { Count: > 0 })
        {
            return targetSet.TargetTypes
                .OrderBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
                .Select(familyType =>
                {
                    var names = new HashSet<string>(setLevelNames, StringComparer.OrdinalIgnoreCase);
                    names.UnionWith(CollectParameterNames(familyType.Parameters));
                    names.UnionWith(queryNames);
                    if (familyParameterNamesByTypeId.TryGetValue(familyType.Identity.Id, out var familyNames))
                    {
                        names.UnionWith(familyNames);
                    }

                    typeContext.TryGetValue(familyType.Identity.Id, out var context);
                    return new ValidationObject(
                        familyType.Identity.Id,
                        "familyType",
                        familyType.Name,
                        context.FamilyName,
                        context.CategoryName ?? categoryName,
                        ParsePlacedInstanceCount(familyType.Metadata),
                        names);
                })
                .ToArray();
        }

        return BuildFamilyValidationObjects(request);
    }

    internal static IReadOnlyList<ValidationObject> BuildFamilyValidationObjects(
        ExistenceContracts.ParameterExistenceRequest request)
    {
        var targetSet = request.TargetSet;
        var setLevelNames = CollectParameterNames(targetSet.TargetParameters);
        var queryNames = CollectParameterNames(request.ParameterQueryResult?.Parameters);
        var categoryName = ResolveCategoryName(request);

        if (targetSet.TargetFamilies is { Count: > 0 })
        {
            return targetSet.TargetFamilies
                .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
                .Select(family =>
                {
                    var names = new HashSet<string>(setLevelNames, StringComparer.OrdinalIgnoreCase);
                    names.UnionWith(CollectParameterNames(family.Parameters));
                    names.UnionWith(CollectParameterNamesFromFamilyTypes(family.FamilyTypes));
                    names.UnionWith(queryNames);
                    return new ValidationObject(
                        family.Identity.Id,
                        "family",
                        family.Name,
                        family.Name,
                        family.Category?.Name ?? categoryName,
                        SumPlacedInstanceCount(family.FamilyTypes),
                        names);
                })
                .ToArray();
        }

        var fallbackNames = new HashSet<string>(setLevelNames, StringComparer.OrdinalIgnoreCase);
        fallbackNames.UnionWith(queryNames);

        return
        [
            new ValidationObject(
                targetSet.TargetSetId,
                "targetSet",
                categoryName ?? "Project",
                null,
                categoryName,
                0,
                fallbackNames)
        ];
    }

    private static IReadOnlyList<ValidationObject> BuildAllValidationObjects(
        ExistenceContracts.ParameterExistenceRequest request)
    {
        var typeObjects = BuildTypeValidationObjects(request);
        if (typeObjects.Count > 0 && typeObjects[0].Kind == "familyType")
        {
            return typeObjects;
        }

        return BuildFamilyValidationObjects(request);
    }

    private static string? ResolveCategoryName(ExistenceContracts.ParameterExistenceRequest request)
    {
        if (request.TargetSet.SelectionMetadata?.TryGetValue("category", out var categoryName) == true
            && !string.IsNullOrWhiteSpace(categoryName))
        {
            return categoryName;
        }

        return null;
    }

    private static Dictionary<string, (string FamilyName, string? CategoryName)> BuildTypeContext(
        IReadOnlyList<NormalizedFamily>? families)
    {
        var context = new Dictionary<string, (string FamilyName, string? CategoryName)>(StringComparer.Ordinal);

        if (families is null)
        {
            return context;
        }

        foreach (var family in families)
        {
            if (family.FamilyTypes is null)
            {
                continue;
            }

            foreach (var familyType in family.FamilyTypes)
            {
                context[familyType.Identity.Id] = (family.Name, family.Category?.Name);
            }
        }

        return context;
    }

    private static int ParsePlacedInstanceCount(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return 0;
        }

        return int.TryParse(metadata.GetValueOrDefault("placedInstanceCount"), out var count) ? count : 0;
    }

    private static int SumPlacedInstanceCount(IReadOnlyList<NormalizedFamilyType>? familyTypes)
    {
        if (familyTypes is null)
        {
            return 0;
        }

        return familyTypes.Sum(type => ParsePlacedInstanceCount(type.Metadata));
    }

    private static Dictionary<string, HashSet<string>> BuildFamilyParameterNamesByTypeId(
        IReadOnlyList<NormalizedFamily>? families)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        if (families is null)
        {
            return map;
        }

        foreach (var family in families)
        {
            var familyUnion = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            familyUnion.UnionWith(CollectParameterNames(family.Parameters));
            familyUnion.UnionWith(CollectParameterNamesFromFamilyTypes(family.FamilyTypes));

            if (family.FamilyTypes is null)
            {
                continue;
            }

            foreach (var familyType in family.FamilyTypes)
            {
                map[familyType.Identity.Id] = familyUnion;
            }
        }

        return map;
    }

    private static HashSet<string> CollectParameterNamesFromFamilyTypes(
        IReadOnlyList<NormalizedFamilyType>? familyTypes)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (familyTypes is null)
        {
            return names;
        }

        foreach (var familyType in familyTypes)
        {
            names.UnionWith(CollectParameterNames(familyType.Parameters));
        }

        return names;
    }

    private static HashSet<string> CollectParameterNames(IReadOnlyList<NormalizedParameter>? parameters)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (parameters is null)
        {
            return names;
        }

        foreach (var parameter in parameters)
        {
            names.Add(parameter.Name);
        }

        return names;
    }

    private static ExistenceContracts.ParameterExistenceStatistics BuildStatistics(
        IReadOnlyList<ExistenceContracts.ParameterExistenceFinding> findings)
    {
        var objectsChecked = findings
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var missingParameters = findings.Count(finding => !finding.Exists);
        var objectsFailed = findings
            .Where(finding => !finding.Exists)
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var objectsPassed = objectsChecked - objectsFailed;

        return new ExistenceContracts.ParameterExistenceStatistics
        {
            ObjectsChecked = objectsChecked,
            ObjectsPassed = objectsPassed,
            ObjectsFailed = objectsFailed,
            MissingParameters = missingParameters
        };
    }

    private static IReadOnlyList<ParameterEngineDiagnostic> BuildDiagnostics(
        string atomId,
        IReadOnlyList<ExistenceContracts.ParameterExistenceFinding> findings)
    {
        var objectsChecked = findings
            .Select(finding => finding.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var missingParameters = findings.Count(finding => !finding.Exists);

        return
        [
            new ParameterEngineDiagnostic
            {
                Code = "ParameterExistence.Completed",
                Message = $"Parameter existence atom '{atomId}' checked {objectsChecked} objects and found {missingParameters} missing parameters.",
                Severity = ParameterEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        ];
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        ExistenceContracts.ParameterExistenceRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["validationOperation"] = "parameter-existence",
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

    private static string BuildEvidenceId(string objectId, string parameterName)
    {
        return $"parameter-missing-{objectId}-{parameterName}".ToLowerInvariant();
    }
}
