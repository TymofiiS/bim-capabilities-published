using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Translators;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

internal static class RelationshipRetrievalSupport
{
    internal const string ProviderId = "revit-adapter-relationship-provider";

    internal static bool RequiresScopeIdentifiers(RelationshipQueryScopeKind scopeKind) =>
        scopeKind is RelationshipQueryScopeKind.SelectedElements
            or RelationshipQueryScopeKind.SelectedFamilies
            or RelationshipQueryScopeKind.SelectedFamilyTypes
            or RelationshipQueryScopeKind.Custom;

    internal static bool IsInvalidQuery(RelationshipQuery query)
    {
        return query.Scope is not null &&
               RequiresScopeIdentifiers(query.Scope.Kind) &&
               query.Scope.ScopeIdentifiers is not { Count: > 0 };
    }

    internal static bool ContainsUnsupportedRelationshipTypes(RelationshipQuery query)
    {
        return query.RelationshipTypes?.Contains(RelationshipType.Custom) == true;
    }

    internal static IEnumerable<IRevitRelationshipCatalogEntry> SelectRelationships(
        IEnumerable<IRevitRelationshipCatalogEntry> entries,
        RelationshipQuery query)
    {
        var candidates = entries;

        if (query.Scope?.Kind == RelationshipQueryScopeKind.SelectedFamilies &&
            query.Scope.ScopeIdentifiers is { Count: > 0 } scopeIdentifiers)
        {
            var scopeIds = scopeIdentifiers.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry => scopeIds.Contains(entry.Handle.SourceId));
        }

        if (query.Scope?.Kind == RelationshipQueryScopeKind.SelectedFamilyTypes &&
            query.Scope.ScopeIdentifiers is { Count: > 0 } familyTypeScopeIds)
        {
            var scopeIds = familyTypeScopeIds.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry =>
                entry.Handle.SourceKind == "familyType" && scopeIds.Contains(entry.Handle.SourceId) ||
                entry.Handle.TargetKind == "familyType" && scopeIds.Contains(entry.Handle.TargetId));
        }

        if (query.SourceObjects is { Count: > 0 } sourceObjects)
        {
            var sourceIds = sourceObjects.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry => sourceIds.Contains(entry.Handle.SourceId));
        }

        if (query.TargetObjects is { Count: > 0 } targetObjects)
        {
            var targetIds = targetObjects.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry => targetIds.Contains(entry.Handle.TargetId));
        }

        if (query.RelationshipTypes is { Count: > 0 } relationshipTypes)
        {
            var requestedTypes = relationshipTypes
                .Where(type => type != RelationshipType.Custom)
                .ToHashSet();

            if (requestedTypes.Count > 0)
            {
                candidates = candidates.Where(entry => requestedTypes.Contains(entry.QueryRelationshipType));
            }
        }

        return candidates
            .OrderBy(entry => entry.Handle.SourceId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Handle.TargetId, StringComparer.Ordinal)
            .ThenBy(entry => entry.QueryRelationshipType);
    }

    internal static IReadOnlyList<NormalizedRelationship> TranslateRelationships(
        IEnumerable<IRevitRelationshipCatalogEntry> entries)
    {
        return entries
            .Select(entry => RelationshipTranslator.Translate(entry.Handle))
            .Where(relationship => relationship is not null)
            .Cast<NormalizedRelationship>()
            .OrderBy(relationship => relationship.Source.Id, StringComparer.Ordinal)
            .ThenBy(relationship => relationship.Target.Id, StringComparer.Ordinal)
            .ToList();
    }

    internal static void AddMissingRelationshipDiagnostics(
        ICollection<RelationshipQueryDiagnostic> diagnostics,
        RelationshipQuery query,
        IReadOnlyList<NormalizedRelationship> relationships)
    {
        if (query.RelationshipTypes is not { Count: > 0 })
        {
            return;
        }

        var foundTypes = relationships
            .Select(relationship => relationship.Metadata?["queryRelationshipType"])
            .Where(type => type is not null)
            .ToHashSet(StringComparer.Ordinal)!;

        var missingTypes = query.RelationshipTypes
            .Where(type => type != RelationshipType.Custom)
            .Where(type => !foundTypes.Contains(type.ToString()))
            .Select(type => type.ToString())
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToList();

        if (missingTypes.Count > 0)
        {
            diagnostics.Add(new RelationshipQueryDiagnostic
            {
                Code = RelationshipRetrievalDiagnostics.RelationshipNotFound,
                Message = "One or more requested relationship types were not found.",
                Severity = RelationshipQueryDiagnosticSeverity.Information,
                Location = "query:relationshipTypes",
                Data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["missingRelationshipTypes"] = string.Join(",", missingTypes)
                }
            });
        }
    }

    internal static void AddEmptyResultDiagnostic(
        ICollection<RelationshipQueryDiagnostic> diagnostics,
        int catalogRelationshipCount)
    {
        diagnostics.Add(new RelationshipQueryDiagnostic
        {
            Code = RelationshipRetrievalDiagnostics.EmptyResult,
            Message = catalogRelationshipCount == 0
                ? "No relationships are available in the active model."
                : "Relationship retrieval returned no results.",
            Severity = RelationshipQueryDiagnosticSeverity.Information,
            Location = "provider:relationship"
        });
    }

    internal static RelationshipQueryStatistics CreateStatistics(
        int totalCandidates,
        IReadOnlyList<NormalizedRelationship> retrievedRelationships)
    {
        var countsByRelationshipType = retrievedRelationships
            .Select(relationship => relationship.Metadata?["queryRelationshipType"])
            .Where(type => type is not null)
            .GroupBy(type => type!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new RelationshipQueryStatistics
        {
            TotalRelationships = totalCandidates,
            RetrievedRelationships = retrievedRelationships.Count,
            FilteredRelationships = Math.Max(0, totalCandidates - retrievedRelationships.Count),
            CountsByRelationshipType = countsByRelationshipType.Count > 0 ? countsByRelationshipType : null
        };
    }

    internal static RelationshipQueryMetadata CreateMetadata(
        RelationshipQuery query,
        DateTimeOffset executedAt,
        int objectsInspected,
        int importedCadRelationships,
        int nestedFamilyRelationships,
        int failedRetrievals)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["objectsInspected"] = objectsInspected.ToString(),
            ["importedCadRelationships"] = importedCadRelationships.ToString(),
            ["nestedFamilyRelationships"] = nestedFamilyRelationships.ToString(),
            ["failedRetrievals"] = failedRetrievals.ToString()
        };

        if (query.Scope is not null)
        {
            properties["scopeKind"] = query.Scope.Kind.ToString();
        }

        if (query.RelationshipTypes is { Count: > 0 })
        {
            properties["relationshipTypeCount"] = query.RelationshipTypes.Count.ToString();
        }

        return new RelationshipQueryMetadata
        {
            CorrelationId = query.CorrelationId,
            ExecutedAt = executedAt,
            ProviderId = ProviderId,
            Properties = properties
        };
    }

    internal static int CountFailedRetrievals(
        RelationshipQuery query,
        IReadOnlyList<NormalizedRelationship> relationships)
    {
        if (query.RelationshipTypes is not { Count: > 0 })
        {
            return 0;
        }

        var foundTypes = relationships
            .Select(relationship => relationship.Metadata?["queryRelationshipType"])
            .Where(type => type is not null)
            .ToHashSet(StringComparer.Ordinal)!;

        return query.RelationshipTypes
            .Count(type => type != RelationshipType.Custom && !foundTypes.Contains(type.ToString()));
    }

    internal static int CountRelationshipsByType(
        IReadOnlyList<NormalizedRelationship> relationships,
        RelationshipType relationshipType)
    {
        var relationshipTypeName = relationshipType.ToString();
        return relationships.Count(relationship =>
            relationship.Metadata is not null &&
            relationship.Metadata.TryGetValue("queryRelationshipType", out var queryType) &&
            string.Equals(queryType, relationshipTypeName, StringComparison.Ordinal));
    }

    internal static RevitRelationshipCatalogEntry CreateEntry(
        string sourceId,
        string sourceKind,
        string targetId,
        string targetKind,
        NormalizedRelationshipType normalizedType,
        RelationshipType queryRelationshipType,
        string referenceType)
    {
        return new RevitRelationshipCatalogEntry
        {
            Handle = new RevitRelationshipHandle(
                sourceId,
                sourceKind,
                targetId,
                targetKind,
                normalizedType,
                queryRelationshipType,
                referenceType),
            QueryRelationshipType = queryRelationshipType
        };
    }
}
