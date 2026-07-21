using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Retrieves Revit families and translates them into normalized BIMCapabilities contracts.
/// </summary>
public sealed class RevitFamilyProvider : IFamilyProvider
{
    private readonly IRevitFamilyCatalog _catalog;
    private readonly IFamilyQueryClock _clock;

    public RevitFamilyProvider(IRevitFamilyCatalog catalog)
        : this(catalog, new SystemFamilyQueryClock())
    {
    }

    internal RevitFamilyProvider(IRevitFamilyCatalog catalog, IFamilyQueryClock clock)
    {
        ArgumentGuard.ThrowIfNull(catalog);
        ArgumentGuard.ThrowIfNull(clock);

        _catalog = catalog;
        _clock = clock;
    }

    public FamilyQueryResult Retrieve(FamilyQuery query)
    {
        ArgumentGuard.ThrowIfNull(query);

        var diagnostics = new List<FamilyQueryDiagnostic>();

        if (FamilyRetrievalSupport.IsInvalidQuery(query))
        {
            diagnostics.Add(new FamilyQueryDiagnostic
            {
                Code = FamilyRetrievalDiagnostics.InvalidQuery,
                Message = "The family query scope requires one or more scope identifiers.",
                Severity = FamilyQueryDiagnosticSeverity.Error,
                Location = "query:scope",
                Data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["scopeKind"] = query.Scope!.Kind.ToString()
                }
            });

            return CreateResult([], [], diagnostics, query, totalFamilies: 0);
        }

        if (query.Filter is not null)
        {
            diagnostics.Add(new FamilyQueryDiagnostic
            {
                Code = FamilyRetrievalDiagnostics.UnsupportedFilter,
                Message = "Family query filter criteria are not evaluated during retrieval.",
                Severity = FamilyQueryDiagnosticSeverity.Information,
                Location = "query:filter"
            });
        }

        var hasInvalidCategories = FamilyRetrievalSupport.TryGetInvalidCategories(query, out var invalidCategories);
        if (hasInvalidCategories)
        {
            diagnostics.Add(new FamilyQueryDiagnostic
            {
                Code = FamilyRetrievalDiagnostics.InvalidCategory,
                Message = "One or more requested categories are not supported by the Revit Adapter.",
                Severity = FamilyQueryDiagnosticSeverity.Error,
                Location = "query:categories",
                Data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["invalidCategories"] = string.Join(",", invalidCategories)
                }
            });

            return CreateResult([], [], diagnostics, query, totalFamilies: 0);
        }

        var catalogFamilies = _catalog.GetFamilies();
        var selectedFamilies = FamilyRetrievalSupport.SelectFamilies(catalogFamilies, query);
        var normalizedFamilies = FamilyRetrievalSupport.TranslateFamilies(selectedFamilies);

        if (query.FamilyTypeNames is { Count: > 0 })
        {
            normalizedFamilies = FamilyRetrievalSupport.ApplyFamilyTypeSelection(normalizedFamilies, query.FamilyTypeNames);
        }

        if (normalizedFamilies.Count == 0)
        {
            FamilyRetrievalSupport.AddEmptyResultDiagnostics(
                diagnostics,
                query,
                catalogFamilies.Count,
                hasInvalidCategories: false);
        }

        var selectedFamilyHandles = selectedFamilies.ToArray();
        var placedInstances = _catalog.GetPlacedInstances(selectedFamilyHandles);

        return CreateResult(normalizedFamilies, placedInstances, diagnostics, query, catalogFamilies.Count);
    }

    private FamilyQueryResult CreateResult(
        IReadOnlyList<NormalizedFamily> families,
        IReadOnlyList<NormalizedPlacedInstance> placedInstances,
        IReadOnlyList<FamilyQueryDiagnostic> diagnostics,
        FamilyQuery query,
        int totalFamilies)
    {
        return new FamilyQueryResult
        {
            Families = families,
            PlacedInstances = placedInstances,
            Diagnostics = diagnostics.Count > 0
                ? diagnostics
                    .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                    .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                    .ToList()
                : null,
            Statistics = FamilyRetrievalSupport.CreateStatistics(totalFamilies, families),
            QueryMetadata = FamilyRetrievalSupport.CreateMetadata(query, _clock.UtcNow)
        };
    }
}
