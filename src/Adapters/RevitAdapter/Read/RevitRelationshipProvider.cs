using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Retrieves Revit relationships and translates them into normalized BIMCapabilities contracts.
/// </summary>
public sealed class RevitRelationshipProvider : IRelationshipProvider
{
    private readonly IRevitRelationshipCatalog _catalog;
    private readonly IFamilyQueryClock _clock;

    public RevitRelationshipProvider(IRevitRelationshipCatalog catalog)
        : this(catalog, new SystemFamilyQueryClock())
    {
    }

    internal RevitRelationshipProvider(IRevitRelationshipCatalog catalog, IFamilyQueryClock clock)
    {
        ArgumentGuard.ThrowIfNull(catalog);
        ArgumentGuard.ThrowIfNull(clock);

        _catalog = catalog;
        _clock = clock;
    }

    public RelationshipQueryResult Retrieve(RelationshipQuery query)
    {
        ArgumentGuard.ThrowIfNull(query);

        var diagnostics = new List<RelationshipQueryDiagnostic>();

        if (RelationshipRetrievalSupport.IsInvalidQuery(query))
        {
            diagnostics.Add(new RelationshipQueryDiagnostic
            {
                Code = RelationshipRetrievalDiagnostics.InvalidQuery,
                Message = "The relationship query scope requires one or more scope identifiers.",
                Severity = RelationshipQueryDiagnosticSeverity.Error,
                Location = "query:scope",
                Data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["scopeKind"] = query.Scope!.Kind.ToString()
                }
            });

            return CreateResult([], diagnostics, query, totalCandidates: 0);
        }

        if (query.Filter is not null)
        {
            diagnostics.Add(new RelationshipQueryDiagnostic
            {
                Code = RelationshipRetrievalDiagnostics.UnsupportedFilter,
                Message = "Relationship query filter criteria are not evaluated during retrieval.",
                Severity = RelationshipQueryDiagnosticSeverity.Information,
                Location = "query:filter"
            });
        }

        if (RelationshipRetrievalSupport.ContainsUnsupportedRelationshipTypes(query))
        {
            diagnostics.Add(new RelationshipQueryDiagnostic
            {
                Code = RelationshipRetrievalDiagnostics.UnsupportedRelationship,
                Message = "Custom relationship retrieval is not supported by the Revit Adapter.",
                Severity = RelationshipQueryDiagnosticSeverity.Error,
                Location = "query:relationshipTypes",
                Data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["unsupportedRelationshipTypes"] = RelationshipType.Custom.ToString()
                }
            });

            return CreateResult([], diagnostics, query, totalCandidates: 0);
        }

        var catalogRelationships = _catalog.GetRelationships();
        var selectedEntries = RelationshipRetrievalSupport.SelectRelationships(catalogRelationships, query).ToList();
        var normalizedRelationships = RelationshipRetrievalSupport.TranslateRelationships(selectedEntries);

        if (normalizedRelationships.Count == 0)
        {
            RelationshipRetrievalSupport.AddEmptyResultDiagnostic(diagnostics, catalogRelationships.Count);
        }

        RelationshipRetrievalSupport.AddMissingRelationshipDiagnostics(diagnostics, query, normalizedRelationships);

        return CreateResult(normalizedRelationships, diagnostics, query, selectedEntries.Count);
    }

    private RelationshipQueryResult CreateResult(
        IReadOnlyList<NormalizedRelationship> relationships,
        IReadOnlyList<RelationshipQueryDiagnostic> diagnostics,
        RelationshipQuery query,
        int totalCandidates)
    {
        var failedRetrievals = RelationshipRetrievalSupport.CountFailedRetrievals(query, relationships);

        return new RelationshipQueryResult
        {
            Relationships = relationships,
            Diagnostics = diagnostics.Count > 0
                ? diagnostics
                    .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                    .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                    .ToList()
                : null,
            Statistics = RelationshipRetrievalSupport.CreateStatistics(totalCandidates, relationships),
            QueryMetadata = RelationshipRetrievalSupport.CreateMetadata(
                query,
                _clock.UtcNow,
                _catalog.ObjectsInspected,
                RelationshipRetrievalSupport.CountRelationshipsByType(relationships, RelationshipType.ImportedCad),
                RelationshipRetrievalSupport.CountRelationshipsByType(relationships, RelationshipType.NestedFamily),
                failedRetrievals)
        };
    }
}
