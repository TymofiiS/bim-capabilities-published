using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;
using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Engines.Family.Atoms.Selection;

internal static class FamilySelectionAtomSupport
{
    internal static SelectionContracts.FamilySelectionResult CreateResult(
        string atomId,
        SelectionContracts.FamilySelectionRequest request,
        IReadOnlyList<NormalizedFamily> selectedFamilies)
    {
        var candidates = request.DiscoveryResult.Families ?? [];
        var orderedSelection = selectedFamilies
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .ToArray();

        return new SelectionContracts.FamilySelectionResult
        {
            AtomId = atomId,
            SelectedFamilies = orderedSelection,
            Statistics = BuildStatistics(candidates, orderedSelection),
            Diagnostics = BuildDiagnostics(atomId, candidates.Count, orderedSelection.Length),
            Metadata = BuildMetadata(request)
        };
    }

    internal static IReadOnlyList<NormalizedFamily> SelectByCategory(
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

    internal static IReadOnlyList<NormalizedFamily> SelectByName(
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

    internal static IReadOnlyList<NormalizedFamily> SelectByParameter(
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

    internal static IReadOnlyList<NormalizedFamily> SelectByRelationship(
        IReadOnlyList<NormalizedFamily> candidates,
        FamilyRelationshipCriteria criteria)
    {
        return candidates
            .Where(family => FamilyMatchesRelationshipCriteria(family, criteria))
            .ToArray();
    }

    internal static IReadOnlyList<NormalizedFamily> SelectCombined(
        IReadOnlyList<NormalizedFamily> candidates,
        FamilySelectionCriteria criteria)
    {
        IEnumerable<NormalizedFamily> selected = candidates;

        if (criteria.Categories is not null)
        {
            selected = SelectByCategory(selected.ToArray(), criteria.Categories);
        }

        if (criteria.Names is not null)
        {
            selected = SelectByName(selected.ToArray(), criteria.Names);
        }

        if (criteria.Parameters is not null)
        {
            selected = SelectByParameter(selected.ToArray(), criteria.Parameters);
        }

        if (criteria.Relationships is not null)
        {
            selected = SelectByRelationship(selected.ToArray(), criteria.Relationships);
        }

        return selected
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .ToArray();
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

    private static SelectionContracts.FamilySelectionStatistics BuildStatistics(
        IReadOnlyList<NormalizedFamily> candidates,
        IReadOnlyList<NormalizedFamily> selectedFamilies)
    {
        var countsByCategory = selectedFamilies
            .GroupBy(family => family.Category?.Name ?? "unknown", StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new SelectionContracts.FamilySelectionStatistics
        {
            CandidateFamilies = candidates.Count,
            SelectedFamilies = selectedFamilies.Count,
            RejectedFamilies = candidates.Count - selectedFamilies.Count,
            CountsByCategory = countsByCategory
        };
    }

    private static IReadOnlyList<FamilyEngineDiagnostic> BuildDiagnostics(
        string atomId,
        int candidateFamilies,
        int selectedFamilies)
    {
        return
        [
            new FamilyEngineDiagnostic
            {
                Code = "FamilySelectionContracts.Completed",
                Message = $"Selection atom '{atomId}' selected {selectedFamilies} of {candidateFamilies} candidate families.",
                Severity = FamilyEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        ];
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(SelectionContracts.FamilySelectionRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["selectionOperation"] = "family-selection",
            ["discoveryAtomId"] = request.DiscoveryResult.AtomId
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
