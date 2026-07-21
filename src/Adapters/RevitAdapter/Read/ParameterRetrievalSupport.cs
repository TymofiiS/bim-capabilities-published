using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Translators;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

internal static class ParameterRetrievalSupport
{
    internal const string ProviderId = "revit-adapter-parameter-provider";

    private static readonly HashSet<string> SupportedStorageTypes = new(StringComparer.Ordinal)
    {
        "Integer",
        "Double",
        "String",
        "ElementId",
        "None"
    };

    internal static bool RequiresScopeIdentifiers(ParameterQueryScopeKind scopeKind) =>
        scopeKind is ParameterQueryScopeKind.SelectedElements
            or ParameterQueryScopeKind.SelectedFamilies
            or ParameterQueryScopeKind.SelectedFamilyTypes
            or ParameterQueryScopeKind.Custom;

    internal static bool IsInvalidQuery(ParameterQuery query)
    {
        if (query.Scope is not null &&
            RequiresScopeIdentifiers(query.Scope.Kind) &&
            query.Scope.ScopeIdentifiers is not { Count: > 0 })
        {
            return true;
        }

        return query.ObjectScope?.FamilyIdentifiers is { Count: 0 }
            || query.ObjectScope?.FamilyTypeIdentifiers is { Count: 0 };
    }

    internal static bool TryGetInvalidCategories(ParameterQuery query, out IReadOnlyList<string> invalidCategories)
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

    internal static IEnumerable<IRevitParameterCatalogEntry> SelectParameters(
        IEnumerable<IRevitParameterCatalogEntry> entries,
        ParameterQuery query)
    {
        var candidates = entries;

        if (query.Scope?.Kind == ParameterQueryScopeKind.SelectedFamilies &&
            query.Scope.ScopeIdentifiers is { Count: > 0 } familyScopeIds)
        {
            var scopeIds = familyScopeIds.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry =>
                entry.FamilyId is not null &&
                scopeIds.Contains(entry.FamilyId));
        }

        if (query.Scope?.Kind == ParameterQueryScopeKind.SelectedFamilyTypes &&
            query.Scope.ScopeIdentifiers is { Count: > 0 } familyTypeScopeIds)
        {
            var scopeIds = familyTypeScopeIds.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry =>
                entry.FamilyTypeId is not null &&
                scopeIds.Contains(entry.FamilyTypeId));
        }

        if (query.ObjectScope?.FamilyIdentifiers is { Count: > 0 } familyIdentifiers)
        {
            var ids = familyIdentifiers.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry =>
                entry.FamilyId is not null &&
                ids.Contains(entry.FamilyId));
        }

        if (query.ObjectScope?.FamilyTypeIdentifiers is { Count: > 0 } familyTypeIdentifiers)
        {
            var ids = familyTypeIdentifiers.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry =>
                entry.FamilyTypeId is not null &&
                ids.Contains(entry.FamilyTypeId));
        }

        if (query.Categories is { Count: > 0 } categories)
        {
            var categoryNames = categories.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry =>
                entry.CategoryName is not null &&
                categoryNames.Contains(entry.CategoryName));
        }

        if (query.ParameterNames is { Count: > 0 } parameterNames)
        {
            var names = parameterNames.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry => names.Contains(entry.Parameter.Name));
        }

        if (query.SharedParameterNames is { Count: > 0 } sharedParameterNames)
        {
            var names = sharedParameterNames.ToHashSet(StringComparer.Ordinal);
            candidates = candidates.Where(entry =>
                entry.Parameter.IsSharedParameter &&
                names.Contains(entry.Parameter.Name));
        }

        if (query.SharedParameterGuids is { Count: > 0 } sharedParameterGuids)
        {
            var guids = sharedParameterGuids.ToHashSet(StringComparer.OrdinalIgnoreCase);
            candidates = candidates.Where(entry =>
                entry.Parameter.Metadata is not null &&
                entry.Parameter.Metadata.TryGetValue("guid", out var guid) &&
                guids.Contains(guid));
        }

        if (query.BuiltInParameterNames is { Count: > 0 } builtInParameterNames)
        {
            var names = builtInParameterNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            candidates = candidates.Where(entry => MatchesBuiltInParameter(entry, names));
        }

        return candidates
            .OrderBy(entry => entry.Parameter.Id, StringComparer.Ordinal);
    }

    internal static TranslationResult TranslateParameters(IEnumerable<IRevitParameterCatalogEntry> entries)
    {
        var parameters = new List<NormalizedParameter>();
        var diagnostics = new List<ParameterQueryDiagnostic>();

        foreach (var entry in entries)
        {
            if (IsUnsupportedStorageType(entry.Parameter.StorageType))
            {
                diagnostics.Add(new ParameterQueryDiagnostic
                {
                    Code = ParameterRetrievalDiagnostics.UnsupportedStorageType,
                    Message = "Parameter storage type is not supported for normalized translation.",
                    Severity = ParameterQueryDiagnosticSeverity.Warning,
                    Location = $"parameter:{entry.Parameter.Name}",
                    Data = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["parameterId"] = entry.Parameter.Id,
                        ["storageType"] = entry.Parameter.StorageType
                    }
                });
            }

            var translated = ParameterTranslator.Translate(entry.Parameter);
            if (translated is not null)
            {
                parameters.Add(translated);
            }
        }

        return new TranslationResult(
            parameters
                .OrderBy(parameter => parameter.Identifier.Id, StringComparer.Ordinal)
                .ToList(),
            diagnostics);
    }

    internal static void AddMissingParameterDiagnostics(
        ICollection<ParameterQueryDiagnostic> diagnostics,
        ParameterQuery query,
        IReadOnlyList<NormalizedParameter> parameters)
    {
        var foundNames = parameters
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.Ordinal);

        if (query.ParameterNames is { Count: > 0 })
        {
            var missingNames = query.ParameterNames
                .Where(name => !foundNames.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            if (missingNames.Count > 0)
            {
                diagnostics.Add(new ParameterQueryDiagnostic
                {
                    Code = ParameterRetrievalDiagnostics.ParameterNotFound,
                    Message = "One or more requested parameters were not found.",
                    Severity = ParameterQueryDiagnosticSeverity.Information,
                    Location = "query:parameterNames",
                    Data = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["missingParameters"] = string.Join(",", missingNames)
                    }
                });
            }
        }

        if (query.SharedParameterNames is { Count: > 0 })
        {
            var missingSharedNames = query.SharedParameterNames
                .Where(name => !foundNames.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            if (missingSharedNames.Count > 0)
            {
                diagnostics.Add(new ParameterQueryDiagnostic
                {
                    Code = ParameterRetrievalDiagnostics.ParameterNotFound,
                    Message = "One or more requested shared parameters were not found.",
                    Severity = ParameterQueryDiagnosticSeverity.Information,
                    Location = "query:sharedParameterNames",
                    Data = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["missingParameters"] = string.Join(",", missingSharedNames)
                    }
                });
            }
        }
    }

    internal static void AddEmptyResultDiagnostic(
        ICollection<ParameterQueryDiagnostic> diagnostics,
        int catalogParameterCount)
    {
        diagnostics.Add(new ParameterQueryDiagnostic
        {
            Code = catalogParameterCount == 0
                ? ParameterRetrievalDiagnostics.EmptyResult
                : ParameterRetrievalDiagnostics.EmptyResult,
            Message = catalogParameterCount == 0
                ? "No parameters are available in the active model."
                : "Parameter retrieval returned no results.",
            Severity = ParameterQueryDiagnosticSeverity.Information,
            Location = "provider:parameter"
        });
    }

    internal static ParameterQueryStatistics CreateStatistics(
        int totalCandidates,
        IReadOnlyList<NormalizedParameter> retrievedParameters,
        int missingParameters)
    {
        var countsByParameterName = retrievedParameters
            .GroupBy(parameter => parameter.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new ParameterQueryStatistics
        {
            TotalParameters = totalCandidates,
            RetrievedParameters = retrievedParameters.Count,
            FilteredParameters = Math.Max(0, totalCandidates - retrievedParameters.Count),
            MissingParameters = missingParameters,
            CountsByParameterName = countsByParameterName.Count > 0 ? countsByParameterName : null
        };
    }

    internal static ParameterQueryMetadata CreateMetadata(
        ParameterQuery query,
        DateTimeOffset executedAt,
        int objectsInspected,
        int sharedParametersRetrieved)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["objectsInspected"] = objectsInspected.ToString(),
            ["sharedParametersRetrieved"] = sharedParametersRetrieved.ToString()
        };

        if (query.Scope is not null)
        {
            properties["scopeKind"] = query.Scope.Kind.ToString();
        }

        if (query.Categories is { Count: > 0 })
        {
            properties["categoryCount"] = query.Categories.Count.ToString();
        }

        if (query.ParameterNames is { Count: > 0 })
        {
            properties["parameterNameCount"] = query.ParameterNames.Count.ToString();
        }

        if (query.SharedParameterFile is not null)
        {
            properties["sharedParameterFile"] = query.SharedParameterFile.FilePath;
        }

        return new ParameterQueryMetadata
        {
            CorrelationId = query.CorrelationId,
            ExecutedAt = executedAt,
            ProviderId = ProviderId,
            Properties = properties
        };
    }

    internal static int CountMissingParameters(ParameterQuery query, IReadOnlyList<NormalizedParameter> parameters)
    {
        var foundNames = parameters
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missingCount = 0;

        if (query.ParameterNames is { Count: > 0 })
        {
            missingCount += query.ParameterNames.Count(name => !foundNames.Contains(name));
        }

        if (query.SharedParameterNames is { Count: > 0 })
        {
            missingCount += query.SharedParameterNames.Count(name => !foundNames.Contains(name));
        }

        return missingCount;
    }

    private static bool MatchesBuiltInParameter(IRevitParameterCatalogEntry entry, ISet<string> builtInNames)
    {
        if (entry.Parameter.Metadata is null ||
            !entry.Parameter.Metadata.TryGetValue("builtInParameter", out var builtInParameter))
        {
            return false;
        }

        return builtInNames.Contains(builtInParameter);
    }

    private static bool IsUnsupportedStorageType(string storageType)
    {
        if (SupportedStorageTypes.Contains(storageType))
        {
            return false;
        }

        return !string.Equals(storageType, "Boolean", StringComparison.OrdinalIgnoreCase);
    }

    internal sealed record TranslationResult(
        IReadOnlyList<NormalizedParameter> Parameters,
        IReadOnlyList<ParameterQueryDiagnostic> Diagnostics);
}
