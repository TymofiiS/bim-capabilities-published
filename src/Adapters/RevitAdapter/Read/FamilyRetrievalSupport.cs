using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Translators;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

internal static class FamilyRetrievalSupport
{
    internal const string ProviderId = "revit-adapter-family-provider";

    internal static bool RequiresScopeIdentifiers(FamilyQueryScopeKind scopeKind) =>
        scopeKind is FamilyQueryScopeKind.SelectedElements
            or FamilyQueryScopeKind.SelectedFamilies
            or FamilyQueryScopeKind.Custom;

    internal static bool TryGetInvalidCategories(FamilyQuery query, out IReadOnlyList<string> invalidCategories)
    {
        invalidCategories = [];

        if (query.Categories is not { Count: > 0 })
        {
            return false;
        }

        invalidCategories = query.Categories
            .Where(category => !SupportedFamilyCategories.Names.Contains(category))
            .OrderBy(category => category, StringComparer.Ordinal)
            .ToList();

        return invalidCategories.Count > 0;
    }

    internal static bool IsInvalidQuery(FamilyQuery query)
    {
        if (query.Scope is null)
        {
            return false;
        }

        if (!RequiresScopeIdentifiers(query.Scope.Kind))
        {
            return false;
        }

        return query.Scope.ScopeIdentifiers is not { Count: > 0 };
    }

    internal static IEnumerable<IRevitFamilyHandle> SelectFamilies(
        IEnumerable<IRevitFamilyHandle> families,
        FamilyQuery query)
    {
        var candidates = families;

        if (query.Scope?.Kind == FamilyQueryScopeKind.SelectedFamilies &&
            query.Scope.ScopeIdentifiers is { Count: > 0 } scopeIdentifiers)
        {
            var scopeIds = scopeIdentifiers.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(family => scopeIds.Contains(family.Id));
        }

        if (query.Categories is { Count: > 0 } categories)
        {
            var categoryNames = categories.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(family =>
                family.Category is not null &&
                categoryNames.Contains(family.Category.Name));
        }

        if (query.FamilyNames is { Count: > 0 } familyNames)
        {
            var names = familyNames.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(family => names.Contains(family.Name));
        }

        return candidates
            .OrderBy(family => family.Id, StringComparer.Ordinal);
    }

    internal static IReadOnlyList<NormalizedFamily> TranslateFamilies(IEnumerable<IRevitFamilyHandle> families)
    {
        return families
            .Select(FamilyTranslator.Translate)
            .Where(family => family is not null)
            .Cast<NormalizedFamily>()
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .ToList();
    }

    internal static IReadOnlyList<NormalizedFamily> ApplyFamilyTypeSelection(
        IReadOnlyList<NormalizedFamily> families,
        IReadOnlyList<string> familyTypeNames)
    {
        var requestedTypeNames = familyTypeNames.ToHashSet(StringComparer.Ordinal);
        var results = new List<NormalizedFamily>();

        foreach (var family in families.OrderBy(candidate => candidate.Identity.Id, StringComparer.Ordinal))
        {
            var matchingTypes = family.FamilyTypes?
                .Where(type => requestedTypeNames.Contains(type.Name))
                .OrderBy(type => type.Identity.Id, StringComparer.Ordinal)
                .ToList();

            if (matchingTypes is not { Count: > 0 })
            {
                continue;
            }

            results.Add(family with { FamilyTypes = matchingTypes });
        }

        return results;
    }

    internal static FamilyQueryStatistics CreateStatistics(
        int totalFamilies,
        IReadOnlyList<NormalizedFamily> retrievedFamilies)
    {
        var countsByCategory = retrievedFamilies
            .Where(family => family.Category is not null)
            .GroupBy(family => family.Category!.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new FamilyQueryStatistics
        {
            TotalFamilies = totalFamilies,
            RetrievedFamilies = retrievedFamilies.Count,
            FilteredFamilies = Math.Max(0, totalFamilies - retrievedFamilies.Count),
            CountsByCategory = countsByCategory.Count > 0 ? countsByCategory : null
        };
    }

    internal static FamilyQueryMetadata CreateMetadata(FamilyQuery query, DateTimeOffset executedAt)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        if (query.Scope is not null)
        {
            properties["scopeKind"] = query.Scope.Kind.ToString();
        }

        if (query.Categories is { Count: > 0 })
        {
            properties["categoryCount"] = query.Categories.Count.ToString();
        }

        if (query.FamilyNames is { Count: > 0 })
        {
            properties["familyNameCount"] = query.FamilyNames.Count.ToString();
        }

        if (query.FamilyTypeNames is { Count: > 0 })
        {
            properties["familyTypeNameCount"] = query.FamilyTypeNames.Count.ToString();
        }

        return new FamilyQueryMetadata
        {
            CorrelationId = query.CorrelationId,
            ExecutedAt = executedAt,
            ProviderId = ProviderId,
            Properties = properties.Count > 0 ? properties : null
        };
    }

    internal static void AddEmptyResultDiagnostics(
        ICollection<FamilyQueryDiagnostic> diagnostics,
        FamilyQuery query,
        int catalogFamilyCount,
        bool hasInvalidCategories)
    {
        if (hasInvalidCategories)
        {
            return;
        }

        if (catalogFamilyCount == 0)
        {
            diagnostics.Add(new FamilyQueryDiagnostic
            {
                Code = FamilyRetrievalDiagnostics.EmptyResult,
                Message = "No families are available in the active model.",
                Severity = FamilyQueryDiagnosticSeverity.Information,
                Location = "provider:family"
            });
            return;
        }

        if (query.FamilyNames is { Count: > 0 })
        {
            diagnostics.Add(new FamilyQueryDiagnostic
            {
                Code = FamilyRetrievalDiagnostics.NoFamiliesFound,
                Message = "No families matched the requested family names.",
                Severity = FamilyQueryDiagnosticSeverity.Information,
                Location = "query:familyNames",
                Data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["requestedNames"] = string.Join(",", query.FamilyNames.OrderBy(name => name, StringComparer.Ordinal))
                }
            });
            return;
        }

        if (query.Categories is { Count: > 0 })
        {
            diagnostics.Add(new FamilyQueryDiagnostic
            {
                Code = FamilyRetrievalDiagnostics.NoFamiliesFound,
                Message = "No families matched the requested categories.",
                Severity = FamilyQueryDiagnosticSeverity.Information,
                Location = "query:categories",
                Data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["requestedCategories"] = string.Join(",", query.Categories.OrderBy(name => name, StringComparer.Ordinal))
                }
            });
            return;
        }

        diagnostics.Add(new FamilyQueryDiagnostic
        {
            Code = FamilyRetrievalDiagnostics.EmptyResult,
            Message = "Family retrieval returned no results.",
            Severity = FamilyQueryDiagnosticSeverity.Information,
            Location = "provider:family"
        });
    }
}
