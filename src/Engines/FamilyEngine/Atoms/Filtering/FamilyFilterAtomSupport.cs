using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;
using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;

namespace BIMCapabilities.Engines.Family.Atoms.Filtering;

internal static class FamilyFilterAtomSupport
{
    internal static FilteringContracts.FamilyFilterResult CreateResult(
        string atomId,
        FilteringContracts.FamilyFilterRequest request,
        IReadOnlyList<NormalizedFamily> filteredFamilies)
    {
        var candidates = request.SelectionResult.SelectedFamilies ?? [];
        var orderedFamilies = filteredFamilies
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .ToArray();

        return new FilteringContracts.FamilyFilterResult
        {
            AtomId = atomId,
            FilteredFamilies = orderedFamilies,
            Statistics = BuildStatistics(candidates, orderedFamilies),
            Diagnostics = BuildDiagnostics(atomId, candidates.Count, orderedFamilies.Length),
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<NormalizedFamily> FilterByCategory(
        IReadOnlyList<NormalizedFamily> candidates,
        FamilyCategoryCriteria criteria)
    {
        var categoryNames = criteria.CategoryNames is { Count: > 0 }
            ? new HashSet<string>(criteria.CategoryNames, StringComparer.OrdinalIgnoreCase)
            : null;
        var categoryIdentifiers = criteria.CategoryIdentifiers is { Count: > 0 }
            ? new HashSet<string>(criteria.CategoryIdentifiers, StringComparer.OrdinalIgnoreCase)
            : null;

        return candidates
            .Where(family =>
                (categoryNames is null || (family.Category is not null && categoryNames.Contains(family.Category.Name)))
                && (categoryIdentifiers is null || (family.Category is not null && categoryIdentifiers.Contains(family.Category.Identifier.Id))))
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedFamily> FilterByName(
        IReadOnlyList<NormalizedFamily> candidates,
        FamilyNameCriteria criteria)
    {
        if (criteria.ExactNames is not { Count: > 0 })
        {
            return candidates;
        }

        var names = new HashSet<string>(criteria.ExactNames, StringComparer.OrdinalIgnoreCase);
        return candidates
            .Where(family => names.Contains(family.Name))
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedFamily> FilterByParameter(
        IReadOnlyList<NormalizedFamily> candidates,
        FamilyParameterCriteria criteria)
    {
        if (criteria.ParameterNames is not { Count: > 0 })
        {
            return candidates;
        }

        var parameterNames = new HashSet<string>(criteria.ParameterNames, StringComparer.OrdinalIgnoreCase);
        var mustExist = criteria.MustExist ?? true;

        return candidates
            .Where(family => FamilyHasParameters(family, parameterNames, mustExist))
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedFamily> FilterByRelationship(
        IReadOnlyList<NormalizedFamily> candidates,
        FamilyRelationshipCriteria criteria)
    {
        return candidates
            .Where(family => FamilyMatchesRelationshipCriteria(family, criteria))
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedFamily> FilterEmptyFamilies(IReadOnlyList<NormalizedFamily> candidates)
    {
        return candidates
            .Where(family => !IsEmptyFamily(family))
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedFamily> FilterUnusedFamilies(IReadOnlyList<NormalizedFamily> candidates)
    {
        return candidates
            .Where(family => !IsUnusedFamily(family))
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedFamily> FilterCombined(
        IReadOnlyList<NormalizedFamily> candidates,
        FamilySelectionCriteria criteria)
    {
        IEnumerable<NormalizedFamily> filtered = candidates;

        if (criteria.Categories is not null)
        {
            filtered = FilterByCategory(filtered.ToArray(), criteria.Categories);
        }

        if (criteria.Names is not null)
        {
            filtered = FilterByName(filtered.ToArray(), criteria.Names);
        }

        if (criteria.Parameters is not null)
        {
            filtered = FilterByParameter(filtered.ToArray(), criteria.Parameters);
        }

        if (criteria.Relationships is not null)
        {
            filtered = FilterByRelationship(filtered.ToArray(), criteria.Relationships);
        }

        if (criteria.Usage is not null)
        {
            filtered = FilterByUsage(filtered.ToArray(), criteria.Usage);
        }

        filtered = FilterEmptyFamilies(filtered.ToArray());

        return filtered
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedFamily> FilterByUsage(
        IReadOnlyList<NormalizedFamily> candidates,
        FamilyUsageCriteria criteria)
    {
        return candidates
            .Where(family => PassesUsageCriteria(family, criteria))
            .ToArray();
    }

    private static bool PassesUsageCriteria(NormalizedFamily family, FamilyUsageCriteria criteria)
    {
        if (criteria.IncludeUnused == false && IsUnusedFamily(family))
        {
            return false;
        }

        if (criteria.IncludeInPlace == false && IsInPlaceFamily(family))
        {
            return false;
        }

        if (criteria.IncludeNested == false && IsNestedFamily(family))
        {
            return false;
        }

        return true;
    }

    private static bool IsEmptyFamily(NormalizedFamily family)
    {
        return family.FamilyTypes is null or { Count: 0 };
    }

    private static bool IsUnusedFamily(NormalizedFamily family)
    {
        if (family.Metadata is null)
        {
            return false;
        }

        if (family.Metadata.TryGetValue("isUnused", out var isUnused)
            && string.Equals(isUnused, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return family.Metadata.TryGetValue("usage", out var usage)
               && string.Equals(usage, "unused", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInPlaceFamily(NormalizedFamily family)
    {
        if (family.Metadata is null)
        {
            return false;
        }

        if (family.Metadata.TryGetValue("isInPlace", out var isInPlace)
            && string.Equals(isInPlace, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return family.Metadata.TryGetValue("usage", out var usage)
               && string.Equals(usage, "in-place", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNestedFamily(NormalizedFamily family)
    {
        if (family.Relationships is { Count: > 0 }
            && family.Relationships.Any(relationship => relationship.RelationshipType == NormalizedRelationshipType.Nested))
        {
            return true;
        }

        return family.Metadata is not null
               && family.Metadata.TryGetValue("usage", out var usage)
               && string.Equals(usage, "nested", StringComparison.OrdinalIgnoreCase);
    }

    private static bool FamilyHasParameters(
        NormalizedFamily family,
        HashSet<string> parameterNames,
        bool mustExist)
    {
        var availableNames = CollectParameterNames(family);
        var matches = parameterNames.All(name => availableNames.Contains(name));

        return mustExist ? matches : !matches;
    }

    private static HashSet<string> CollectParameterNames(NormalizedFamily family)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (family.Parameters is not null)
        {
            foreach (var parameter in family.Parameters)
            {
                names.Add(parameter.Name);
            }
        }

        if (family.FamilyTypes is not null)
        {
            foreach (var familyType in family.FamilyTypes)
            {
                if (familyType.Parameters is null)
                {
                    continue;
                }

                foreach (var parameter in familyType.Parameters)
                {
                    names.Add(parameter.Name);
                }
            }
        }

        return names;
    }

    private static bool FamilyMatchesRelationshipCriteria(
        NormalizedFamily family,
        FamilyRelationshipCriteria criteria)
    {
        if (family.Relationships is not { Count: > 0 })
        {
            return criteria.RelationshipTypes is not { Count: > 0 };
        }

        return family.Relationships.Any(relationship =>
            (criteria.RelationshipTypes is null
             || criteria.RelationshipTypes.Contains(relationship.RelationshipType))
            && (string.IsNullOrWhiteSpace(criteria.TargetKind)
                || string.Equals(relationship.Target.Kind, criteria.TargetKind, StringComparison.OrdinalIgnoreCase)));
    }

    private static FilteringContracts.FamilyFilterStatistics BuildStatistics(
        IReadOnlyList<NormalizedFamily> candidates,
        IReadOnlyList<NormalizedFamily> filteredFamilies)
    {
        var countsByCategory = filteredFamilies
            .GroupBy(family => family.Category?.Name ?? "unknown", StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new FilteringContracts.FamilyFilterStatistics
        {
            CandidateFamilies = candidates.Count,
            FilteredFamilies = filteredFamilies.Count,
            RemovedFamilies = candidates.Count - filteredFamilies.Count,
            CountsByCategory = countsByCategory
        };
    }

    private static IReadOnlyList<FamilyEngineDiagnostic> BuildDiagnostics(
        string atomId,
        int candidateFamilies,
        int filteredFamilies)
    {
        return
        [
            new FamilyEngineDiagnostic
            {
                Code = "FamilyFilter.Completed",
                Message = $"Filter atom '{atomId}' retained {filteredFamilies} of {candidateFamilies} selected families.",
                Severity = FamilyEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        ];
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(FilteringContracts.FamilyFilterRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["filterOperation"] = "family-filter",
            ["selectionAtomId"] = request.SelectionResult.AtomId
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
