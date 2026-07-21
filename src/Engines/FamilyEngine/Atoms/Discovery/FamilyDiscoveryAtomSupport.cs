using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Family.Discovery;

namespace BIMCapabilities.Engines.Family.Atoms.Discovery;

internal static class FamilyDiscoveryAtomSupport
{
    internal static FamilyDiscoveryResult CreateResult(
        string atomId,
        FamilyDiscoveryRequest request,
        FamilyQueryResult providerResult,
        IReadOnlyList<NormalizedFamilyType>? familyTypes = null)
    {
        var families = providerResult.Families;
        var resolvedFamilyTypes = familyTypes ?? CollectFamilyTypes(families);
        var diagnostics = BuildDiagnostics(atomId, providerResult.Diagnostics);

        return new FamilyDiscoveryResult
        {
            AtomId = atomId,
            Families = families,
            FamilyTypes = resolvedFamilyTypes,
            PlacedInstances = providerResult.PlacedInstances,
            Statistics = BuildStatistics(providerResult, families, resolvedFamilyTypes),
            Diagnostics = diagnostics,
            Metadata = BuildMetadata(request, providerResult)
        };
    }

    private static IReadOnlyList<NormalizedFamilyType> CollectFamilyTypes(IReadOnlyList<NormalizedFamily> families)
    {
        return families
            .SelectMany(family => family.FamilyTypes ?? [])
            .OrderBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static FamilyDiscoveryStatistics BuildStatistics(
        FamilyQueryResult providerResult,
        IReadOnlyList<NormalizedFamily> families,
        IReadOnlyList<NormalizedFamilyType> familyTypes)
    {
        var countsByCategory = families
            .GroupBy(family => family.Category?.Name ?? "unknown", StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new FamilyDiscoveryStatistics
        {
            DiscoveredFamilies = families.Count,
            DiscoveredFamilyTypes = familyTypes.Count,
            ProviderRetrievedFamilies = providerResult.Statistics?.RetrievedFamilies ?? families.Count,
            CountsByCategory = countsByCategory
        };
    }

    private static IReadOnlyList<FamilyEngineDiagnostic> BuildDiagnostics(
        string atomId,
        IReadOnlyList<FamilyQueryDiagnostic>? providerDiagnostics)
    {
        var diagnostics = new List<FamilyEngineDiagnostic>
        {
            new()
            {
                Code = "FamilyDiscovery.Completed",
                Message = $"Discovery atom '{atomId}' completed using adapter family provider.",
                Severity = FamilyEngineDiagnosticSeverity.Information,
                Location = $"atom:{atomId}"
            }
        };

        if (providerDiagnostics is null)
        {
            return diagnostics;
        }

        foreach (var diagnostic in providerDiagnostics.OrderBy(entry => entry.Code, StringComparer.Ordinal))
        {
            diagnostics.Add(new FamilyEngineDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = MapSeverity(diagnostic.Severity),
                Location = diagnostic.Location,
                Data = diagnostic.Data
            });
        }

        return diagnostics;
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        FamilyDiscoveryRequest request,
        FamilyQueryResult providerResult)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["discoveryOperation"] = "family-discovery"
        };

        if (!string.IsNullOrWhiteSpace(request.RuleId))
        {
            metadata["ruleId"] = request.RuleId;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata["correlationId"] = request.CorrelationId;
        }

        if (!string.IsNullOrWhiteSpace(providerResult.QueryMetadata?.ProviderId))
        {
            metadata["providerId"] = providerResult.QueryMetadata.ProviderId;
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

    private static FamilyEngineDiagnosticSeverity MapSeverity(FamilyQueryDiagnosticSeverity severity)
    {
        return severity switch
        {
            FamilyQueryDiagnosticSeverity.Warning => FamilyEngineDiagnosticSeverity.Warning,
            FamilyQueryDiagnosticSeverity.Error => FamilyEngineDiagnosticSeverity.Error,
            _ => FamilyEngineDiagnosticSeverity.Information
        };
    }
}
