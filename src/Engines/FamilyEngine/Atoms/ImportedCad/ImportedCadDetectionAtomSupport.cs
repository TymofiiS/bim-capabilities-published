using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Family;
using ImportedCadContracts = BIMCapabilities.Contracts.Engines.Family.ImportedCad;

namespace BIMCapabilities.Engines.Family.Atoms.ImportedCad;

internal static class ImportedCadDetectionAtomSupport
{
    internal const string DefaultExecutedAt = "2026-06-20T08:00:00+00:00";

    internal static ImportedCadContracts.ImportedCadDetectionResult CreateResult(
        string atomId,
        ImportedCadContracts.ImportedCadDetectionRequest request,
        IReadOnlyList<ImportedCadContracts.ImportedCadFinding> findings,
        IReadOnlyList<EvidenceRecord> evidence)
    {
        var orderedFindings = findings
            .OrderBy(finding => finding.Family.Identity.Id, StringComparer.Ordinal)
            .ToArray();
        var affectedFamilies = orderedFindings
            .Where(finding => finding.HasImportedCad)
            .Select(finding => finding.Family)
            .ToArray();
        var importedCadReferences = orderedFindings.Sum(finding => finding.ImportedCadRelationships?.Count ?? 0);

        return new ImportedCadContracts.ImportedCadDetectionResult
        {
            AtomId = atomId,
            AffectedFamilies = affectedFamilies,
            Findings = orderedFindings,
            Evidence = evidence,
            Statistics = BuildStatistics(orderedFindings, importedCadReferences),
            Diagnostics = BuildDiagnostics(atomId, orderedFindings.Length, affectedFamilies.Length, importedCadReferences),
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<ImportedCadContracts.ImportedCadFinding> AnalyzeFamilies(
        ImportedCadContracts.ImportedCadDetectionRequest request)
    {
        var failureSeverity = request.Configuration?.FailureSeverity ?? EvidenceSeverity.Error;
        var families = request.Families ?? [];

        return families
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .Select(family =>
            {
                var importedCadRelationships = FindImportedCadRelationships(family, request.RelationshipQueryResult);
                var hasImportedCad = importedCadRelationships.Count > 0;

                return new ImportedCadContracts.ImportedCadFinding
                {
                    Family = family,
                    HasImportedCad = hasImportedCad,
                    ImportedCadRelationships = hasImportedCad ? importedCadRelationships : null,
                    Severity = hasImportedCad ? failureSeverity : null
                };
            })
            .ToArray();
    }

    internal static IReadOnlyList<EvidenceRecord> BuildEvidence(
        ImportedCadContracts.ImportedCadDetectionRequest request,
        string atomId,
        IReadOnlyList<ImportedCadContracts.ImportedCadFinding> findings)
    {
        var executedAt = request.ExecutedAt ?? DateTimeOffset.Parse(DefaultExecutedAt);
        var evidence = new List<EvidenceRecord>();

        foreach (var finding in findings.Where(candidate => candidate.HasImportedCad))
        {
            foreach (var relationship in finding.ImportedCadRelationships ?? [])
            {
                evidence.Add(new EvidenceRecord
                {
                    EvidenceId = BuildEvidenceId(finding.Family.Identity.Id, relationship.Target.Id),
                    Timestamp = executedAt,
                    Source = new EvidenceSource
                    {
                        EngineId = "family-engine",
                        AtomId = atomId,
                        RuleId = request.RuleId,
                        CapabilityId = atomId
                    },
                    Target = new EvidenceTarget
                    {
                        TargetType = "Family",
                        TargetId = finding.Family.Identity.Id,
                        TargetName = finding.Family.Name,
                        TargetSetDescription = finding.Family.Category?.Name
                    },
                    Category = EvidenceCategory.Validation,
                    Severity = finding.Severity ?? EvidenceSeverity.Error,
                    Message = $"Family '{finding.Family.Name}' contains imported CAD reference '{relationship.Target.Id}'.",
                    StructuredData = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["familyId"] = finding.Family.Identity.Id,
                        ["familyName"] = finding.Family.Name,
                        ["importedCadReferenceId"] = relationship.Target.Id,
                        ["relationshipType"] = relationship.RelationshipType.ToString(),
                        ["queryRelationshipType"] = relationship.Metadata?["queryRelationshipType"] ?? RelationshipType.ImportedCad.ToString()
                    }
                });
            }
        }

        return evidence
            .OrderBy(record => record.EvidenceId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedRelationship> FindImportedCadRelationships(
        NormalizedFamily family,
        RelationshipQueryResult? relationshipQueryResult)
    {
        var relationships = CollectFamilyRelationships(family, relationshipQueryResult);

        return relationships
            .Where(IsImportedCadRelationship)
            .OrderBy(relationship => relationship.Target.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<NormalizedRelationship> CollectFamilyRelationships(
        NormalizedFamily family,
        RelationshipQueryResult? relationshipQueryResult)
    {
        var relationships = new Dictionary<string, NormalizedRelationship>(StringComparer.Ordinal);

        if (family.Relationships is not null)
        {
            foreach (var relationship in family.Relationships)
            {
                relationships[BuildRelationshipKey(relationship)] = relationship;
            }
        }

        if (relationshipQueryResult?.Relationships is not null)
        {
            foreach (var relationship in relationshipQueryResult.Relationships)
            {
                if (!string.Equals(relationship.Source.Id, family.Identity.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                relationships[BuildRelationshipKey(relationship)] = relationship;
            }
        }

        return relationships.Values
            .OrderBy(relationship => relationship.Target.Id, StringComparer.Ordinal)
            .ToArray();
    }

    internal static bool IsImportedCadRelationship(NormalizedRelationship relationship)
    {
        if (string.Equals(relationship.Target.Kind, "importedCad", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return relationship.Metadata is not null
               && relationship.Metadata.TryGetValue("queryRelationshipType", out var queryRelationshipType)
               && string.Equals(queryRelationshipType, RelationshipType.ImportedCad.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildRelationshipKey(NormalizedRelationship relationship)
    {
        return $"{relationship.Source.Id}|{relationship.Target.Id}|{relationship.RelationshipType}";
    }

    private static string BuildEvidenceId(string familyId, string importedCadReferenceId)
    {
        return $"imported-cad-{familyId}-{importedCadReferenceId}";
    }

    private static ImportedCadContracts.ImportedCadDetectionStatistics BuildStatistics(
        IReadOnlyList<ImportedCadContracts.ImportedCadFinding> findings,
        int importedCadReferencesFound)
    {
        var failedFamilies = findings.Count(finding => finding.HasImportedCad);

        return new ImportedCadContracts.ImportedCadDetectionStatistics
        {
            FamiliesChecked = findings.Count,
            FamiliesPassed = findings.Count - failedFamilies,
            FamiliesFailed = failedFamilies,
            ImportedCadReferencesFound = importedCadReferencesFound
        };
    }

    private static IReadOnlyList<FamilyEngineDiagnostic> BuildDiagnostics(
        string atomId,
        int familiesChecked,
        int familiesFailed,
        int importedCadReferencesFound)
    {
        return
        [
            new FamilyEngineDiagnostic
            {
                Code = "ImportedCadDetection.Completed",
                Message = $"Imported CAD detection atom '{atomId}' checked {familiesChecked} families, found {importedCadReferencesFound} imported CAD references in {familiesFailed} families.",
                Severity = FamilyEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        ];
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        ImportedCadContracts.ImportedCadDetectionRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["detectionOperation"] = "imported-cad-detection",
            ["failureSeverity"] = (request.Configuration?.FailureSeverity ?? EvidenceSeverity.Error).ToString()
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
}
