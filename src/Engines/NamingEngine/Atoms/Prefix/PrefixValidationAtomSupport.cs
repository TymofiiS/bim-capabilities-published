using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Naming;
using PrefixContracts = BIMCapabilities.Contracts.Engines.Naming.Prefix;

namespace BIMCapabilities.Engines.Naming.Atoms.Prefix;

internal static class PrefixValidationAtomSupport
{
    internal const string DefaultExecutedAt = "2026-06-20T11:00:00+00:00";

    internal sealed record ValidationObject(
        string Id,
        string Kind,
        string? Name);

    internal static PrefixContracts.PrefixValidationResult CreateResult(
        string atomId,
        PrefixContracts.PrefixValidationRequest request,
        IReadOnlyList<PrefixContracts.PrefixValidationFinding> findings,
        IReadOnlyList<EvidenceRecord> evidence)
    {
        var orderedFindings = findings
            .OrderBy(finding => finding.ObjectKind, StringComparer.Ordinal)
            .ThenBy(finding => finding.ObjectId, StringComparer.Ordinal)
            .ToArray();

        return new PrefixContracts.PrefixValidationResult
        {
            AtomId = atomId,
            Findings = orderedFindings,
            Evidence = evidence,
            Statistics = BuildStatistics(orderedFindings),
            Diagnostics = BuildDiagnostics(atomId, orderedFindings, request),
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<PrefixContracts.PrefixValidationFinding> AnalyzePrefixes(
        PrefixContracts.PrefixValidationRequest request)
    {
        var requiredPrefixes = (request.RequiredPrefixes ?? [])
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
            .OrderBy(prefix => prefix, StringComparer.Ordinal)
            .ToArray();
        var objects = BuildValidationObjects(request);
        var findings = new List<PrefixContracts.PrefixValidationFinding>();

        foreach (var validationObject in objects)
        {
            var evaluation = EvaluatePrefix(validationObject.Name, requiredPrefixes, request.CaseSensitive);

            findings.Add(new PrefixContracts.PrefixValidationFinding
            {
                ObjectId = validationObject.Id,
                ObjectKind = validationObject.Kind,
                ObjectName = validationObject.Name,
                Status = evaluation.Status,
                Passed = evaluation.Passed,
                MatchedPrefix = evaluation.MatchedPrefix,
                RequiredPrefixes = requiredPrefixes
            });
        }

        return findings;
    }

    internal static (PrefixContracts.PrefixValidationStatus Status, bool Passed, string? MatchedPrefix) EvaluatePrefix(
        string? name,
        IReadOnlyList<string> requiredPrefixes,
        bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (PrefixContracts.PrefixValidationStatus.EmptyName, false, null);
        }

        var trimmedName = name.Trim();

        foreach (var prefix in requiredPrefixes)
        {
            if (StartsWithPrefix(trimmedName, prefix, caseSensitive))
            {
                return (PrefixContracts.PrefixValidationStatus.Valid, true, prefix);
            }
        }

        if (caseSensitive)
        {
            foreach (var prefix in requiredPrefixes)
            {
                if (StartsWithPrefix(trimmedName, prefix, caseSensitive: false)
                    && !StartsWithPrefix(trimmedName, prefix, caseSensitive: true))
                {
                    return (PrefixContracts.PrefixValidationStatus.InvalidPrefix, false, null);
                }
            }
        }

        return (PrefixContracts.PrefixValidationStatus.MissingPrefix, false, null);
    }

    internal static IReadOnlyList<EvidenceRecord> BuildEvidence(
        PrefixContracts.PrefixValidationRequest request,
        string atomId,
        IReadOnlyList<PrefixContracts.PrefixValidationFinding> findings)
    {
        var executedAt = request.ExecutedAt ?? DateTimeOffset.Parse(DefaultExecutedAt);
        var evidence = new List<EvidenceRecord>();

        foreach (var finding in findings.Where(candidate => !candidate.Passed))
        {
            evidence.Add(new EvidenceRecord
            {
                EvidenceId = BuildEvidenceId(finding.ObjectId, finding.Status),
                Timestamp = executedAt,
                Source = new EvidenceSource
                {
                    EngineId = "naming-engine",
                    AtomId = atomId,
                    RuleId = request.RuleId,
                    CapabilityId = atomId
                },
                Target = new EvidenceTarget
                {
                    TargetType = finding.ObjectKind ?? "object",
                    TargetId = finding.ObjectId,
                    TargetName = finding.ObjectName,
                    TargetSetDescription = ResolveCategoryName(request) ?? request.TargetSet.TargetSetId
                },
                Category = EvidenceCategory.Validation,
                Severity = EvidenceSeverity.Error,
                Message = BuildEvidenceMessage(finding),
                StructuredData = BuildStructuredData(finding, request)
            });
        }

        return evidence
            .OrderBy(record => record.EvidenceId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<ValidationObject> BuildValidationObjects(
        PrefixContracts.PrefixValidationRequest request)
    {
        var targetSet = request.TargetSet;
        var includeFamilies = ShouldValidateFamilies(request.PrefixFixScope);
        var includeTypes = ShouldValidateTypes(request.PrefixFixScope);
        var objects = new List<ValidationObject>();

        if (includeFamilies && targetSet.TargetFamilies is { Count: > 0 })
        {
            objects.AddRange(targetSet.TargetFamilies
                .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
                .Select(family => new ValidationObject(
                    family.Identity.Id,
                    "family",
                    family.Name)));
        }

        if (includeTypes && targetSet.TargetTypes is { Count: > 0 })
        {
            objects.AddRange(targetSet.TargetTypes
                .OrderBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
                .Select(familyType => new ValidationObject(
                    familyType.Identity.Id,
                    "familyType",
                    familyType.Name)));
        }

        if (objects.Count == 0)
        {
            objects.Add(new ValidationObject(
                targetSet.TargetSetId,
                "targetSet",
                targetSet.TargetSetId));
        }

        return objects;
    }

    private static bool ShouldValidateFamilies(PrefixFixScope prefixFixScope)
    {
        return prefixFixScope == PrefixFixScope.None || prefixFixScope.HasFlag(PrefixFixScope.Family);
    }

    private static bool ShouldValidateTypes(PrefixFixScope prefixFixScope)
    {
        return prefixFixScope == PrefixFixScope.None || prefixFixScope.HasFlag(PrefixFixScope.Type);
    }

    private static bool StartsWithPrefix(string name, string prefix, bool caseSensitive)
    {
        return caseSensitive
            ? name.StartsWith(prefix, StringComparison.Ordinal)
            : name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static PrefixContracts.PrefixValidationStatistics BuildStatistics(
        IReadOnlyList<PrefixContracts.PrefixValidationFinding> findings)
    {
        var objectsChecked = findings.Count;
        var objectsFailed = findings.Count(finding => !finding.Passed);
        var missingPrefixCount = findings.Count(finding =>
            finding.Status is PrefixContracts.PrefixValidationStatus.EmptyName
                or PrefixContracts.PrefixValidationStatus.MissingPrefix);
        var invalidPrefixCount = findings.Count(finding =>
            finding.Status == PrefixContracts.PrefixValidationStatus.InvalidPrefix);

        return new PrefixContracts.PrefixValidationStatistics
        {
            ObjectsChecked = objectsChecked,
            ObjectsPassed = objectsChecked - objectsFailed,
            ObjectsFailed = objectsFailed,
            MissingPrefixCount = missingPrefixCount,
            InvalidPrefixCount = invalidPrefixCount
        };
    }

    private static IReadOnlyList<NamingEngineDiagnostic> BuildDiagnostics(
        string atomId,
        IReadOnlyList<PrefixContracts.PrefixValidationFinding> findings,
        PrefixContracts.PrefixValidationRequest request)
    {
        var missingPrefixCount = findings.Count(finding =>
            finding.Status is PrefixContracts.PrefixValidationStatus.EmptyName
                or PrefixContracts.PrefixValidationStatus.MissingPrefix);
        var invalidPrefixCount = findings.Count(finding =>
            finding.Status == PrefixContracts.PrefixValidationStatus.InvalidPrefix);

        return
        [
            new NamingEngineDiagnostic
            {
                Code = "PrefixValidation.Completed",
                Message = $"Prefix validation atom '{atomId}' checked {findings.Count} objects using prefixes [{string.Join(", ", request.RequiredPrefixes)}], found {missingPrefixCount} missing prefixes and {invalidPrefixCount} invalid prefixes.",
                Severity = NamingEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        ];
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        PrefixContracts.PrefixValidationRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["validationOperation"] = "prefix-validation",
            ["targetSetId"] = request.TargetSet.TargetSetId,
            ["caseSensitive"] = request.CaseSensitive.ToString()
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
        PrefixContracts.PrefixValidationFinding finding,
        PrefixContracts.PrefixValidationRequest request)
    {
        var structuredData = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["objectId"] = finding.ObjectId,
            ["objectKind"] = finding.ObjectKind ?? "object",
            ["objectName"] = finding.ObjectName ?? string.Empty,
            ["validationStatus"] = finding.Status.ToString(),
            ["caseSensitive"] = request.CaseSensitive.ToString(),
            ["requiredPrefixes"] = string.Join(",", finding.RequiredPrefixes ?? [])
        };

        var categoryName = ResolveCategoryName(request);
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            structuredData["categoryName"] = categoryName;
        }

        return structuredData;
    }

    private static string? ResolveCategoryName(PrefixContracts.PrefixValidationRequest request)
    {
        if (request.TargetSet.SelectionMetadata?.TryGetValue("category", out var categoryName) == true
            && !string.IsNullOrWhiteSpace(categoryName))
        {
            return categoryName;
        }

        if (request.Metadata?.TryGetValue("category", out categoryName) == true
            && !string.IsNullOrWhiteSpace(categoryName))
        {
            return categoryName;
        }

        return null;
    }

    private static string BuildEvidenceMessage(PrefixContracts.PrefixValidationFinding finding)
    {
        return finding.Status switch
        {
            PrefixContracts.PrefixValidationStatus.EmptyName =>
                $"Object '{finding.ObjectId}' has an empty name.",
            PrefixContracts.PrefixValidationStatus.InvalidPrefix =>
                $"Object name '{finding.ObjectName ?? finding.ObjectId}' uses an incorrect prefix.",
            PrefixContracts.PrefixValidationStatus.MissingPrefix =>
                $"Object name '{finding.ObjectName ?? finding.ObjectId}' is missing a required prefix.",
            _ => $"Prefix validation failed for '{finding.ObjectName ?? finding.ObjectId}'."
        };
    }

    private static string BuildEvidenceId(string objectId, PrefixContracts.PrefixValidationStatus status)
    {
        return $"prefix-{status}-{objectId}".ToLowerInvariant();
    }
}
