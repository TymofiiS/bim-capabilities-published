using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Family.Discovery;
using BIMCapabilities.Contracts.Engines.Family.Filtering;
using BIMCapabilities.Contracts.Engines.Family.ImportedCad;
using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;
using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Engines.Family.Atoms.Discovery;
using BIMCapabilities.Engines.Family.Atoms.Filtering;
using BIMCapabilities.Engines.Family.Atoms.ImportedCad;
using BIMCapabilities.Engines.Family.Atoms.Selection;
using TargetSetContracts = BIMCapabilities.Contracts.Engines.Family.TargetSet;

namespace BIMCapabilities.Engines.Family.Generation;

internal static class FamilyTargetSetGeneratorSupport
{
    internal static TargetSetContracts.FamilyTargetSetResult Generate(
        string generatorId,
        TargetSetContracts.FamilyTargetSetRequest request,
        IFamilyProvider familyProvider)
    {
        var discoveryResult = DiscoverFamilies(request, familyProvider);
        var selectionResult = SelectFamilies(request, discoveryResult);
        var filterResult = FilterFamilies(request, selectionResult);
        var detectionResult = DetectImportedCad(request, filterResult.FilteredFamilies);
        var targetFamilies = ApplyComplianceCriteria(
            filterResult.FilteredFamilies,
            detectionResult,
            request.Definition.ComplianceCriteria);

        var placedInstances = FilterPlacedInstances(discoveryResult.PlacedInstances, targetFamilies);
        var targetSet = BuildTargetSet(request.Definition, targetFamilies, placedInstances, request.CorrelationId);
        var diagnostics = CollectDiagnostics(discoveryResult, selectionResult, filterResult, detectionResult, generatorId);

        return new TargetSetContracts.FamilyTargetSetResult
        {
            GeneratorId = generatorId,
            TargetSet = targetSet,
            Statistics = BuildStatistics(
                discoveryResult,
                selectionResult,
                filterResult,
                detectionResult,
                targetFamilies),
            Evidence = detectionResult.Evidence,
            Diagnostics = diagnostics,
            Metadata = BuildMetadata(request, targetSet.TargetSetId)
        };
    }

    private static FamilyDiscoveryResult DiscoverFamilies(
        TargetSetContracts.FamilyTargetSetRequest request,
        IFamilyProvider familyProvider)
    {
        var discoveryRequest = new FamilyDiscoveryRequest
        {
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        };

        return new DiscoverAllFamiliesAtom().Discover(discoveryRequest, familyProvider);
    }

    private static SelectionContracts.FamilySelectionResult SelectFamilies(
        TargetSetContracts.FamilyTargetSetRequest request,
        FamilyDiscoveryResult discoveryResult)
    {
        if (request.Definition.SelectionCriteria is null)
        {
            var families = discoveryResult.Families ?? [];
            return new SelectionContracts.FamilySelectionResult
            {
                AtomId = "family.selection.pass-through",
                SelectedFamilies = families,
                Statistics = new SelectionContracts.FamilySelectionStatistics
                {
                    CandidateFamilies = families.Count,
                    SelectedFamilies = families.Count,
                    RejectedFamilies = 0
                }
            };
        }

        return new SelectFamiliesCombinedAtom().Select(new SelectionContracts.FamilySelectionRequest
        {
            DiscoveryResult = discoveryResult,
            Criteria = request.Definition.SelectionCriteria,
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        });
    }

    private static FilteringContracts.FamilyFilterResult FilterFamilies(
        TargetSetContracts.FamilyTargetSetRequest request,
        SelectionContracts.FamilySelectionResult selectionResult)
    {
        if (request.Definition.FilteringCriteria is null)
        {
            var families = selectionResult.SelectedFamilies ?? [];
            return new FilteringContracts.FamilyFilterResult
            {
                AtomId = "family.filter.pass-through",
                FilteredFamilies = families,
                Statistics = new FilteringContracts.FamilyFilterStatistics
                {
                    CandidateFamilies = families.Count,
                    FilteredFamilies = families.Count,
                    RemovedFamilies = 0
                }
            };
        }

        return new FilterFamiliesCombinedAtom().Filter(new FilteringContracts.FamilyFilterRequest
        {
            SelectionResult = selectionResult,
            Criteria = request.Definition.FilteringCriteria,
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        });
    }

    private static ImportedCadDetectionResult DetectImportedCad(
        TargetSetContracts.FamilyTargetSetRequest request,
        IReadOnlyList<NormalizedFamily> families)
    {
        return new ImportedCadDetectionAtom().Detect(new ImportedCadDetectionRequest
        {
            Families = families,
            RelationshipQueryResult = request.RelationshipQueryResult,
            Configuration = request.ImportedCadConfiguration,
            ExecutedAt = request.ExecutedAt,
            RuleId = request.RuleId,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata
        });
    }

    internal static IReadOnlyList<NormalizedFamily> ApplyComplianceCriteria(
        IReadOnlyList<NormalizedFamily> families,
        ImportedCadDetectionResult detectionResult,
        TargetSetContracts.TargetSetComplianceCriteria? complianceCriteria)
    {
        var mode = complianceCriteria?.ImportedCadMode ?? TargetSetContracts.ImportedCadComplianceMode.None;
        if (mode == TargetSetContracts.ImportedCadComplianceMode.None)
        {
            return families
                .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
                .ToArray();
        }

        var findingsByFamilyId = (detectionResult.Findings ?? [])
            .ToDictionary(finding => finding.Family.Identity.Id, StringComparer.Ordinal);

        return families
            .Where(family =>
            {
                if (!findingsByFamilyId.TryGetValue(family.Identity.Id, out var finding))
                {
                    return mode == TargetSetContracts.ImportedCadComplianceMode.ExcludeImportedCad;
                }

                return mode switch
                {
                    TargetSetContracts.ImportedCadComplianceMode.RequireImportedCad => finding.HasImportedCad,
                    TargetSetContracts.ImportedCadComplianceMode.ExcludeImportedCad => !finding.HasImportedCad,
                    _ => true
                };
            })
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static FamilyTargetSet BuildTargetSet(
        TargetSetContracts.TargetSetDefinition definition,
        IReadOnlyList<NormalizedFamily> families,
        IReadOnlyList<NormalizedPlacedInstance> placedInstances,
        string? correlationId)
    {
        var familyTypes = families
            .SelectMany(family => family.FamilyTypes ?? [])
            .GroupBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
            .ToArray();

        var categories = families
            .Where(family => family.Category is not null)
            .Select(family => family.Category!)
            .GroupBy(category => category.Identifier.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(category => category.Identifier.Id, StringComparer.Ordinal)
            .ToArray();

        var relationships = families
            .SelectMany(family => family.Relationships ?? [])
            .GroupBy(relationship => $"{relationship.Source.Id}|{relationship.Target.Id}|{relationship.RelationshipType}", StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(relationship => relationship.Target.Id, StringComparer.Ordinal)
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["targetSetName"] = definition.Name
        };

        if (!string.IsNullOrWhiteSpace(definition.Description))
        {
            metadata["targetSetDescription"] = definition.Description;
        }

        if (definition.Metadata is not null)
        {
            foreach (var pair in definition.Metadata.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return new FamilyTargetSet
        {
            TargetSetId = BuildTargetSetId(definition.Name, correlationId),
            Families = families,
            FamilyTypes = familyTypes,
            PlacedInstances = placedInstances,
            Categories = categories,
            Relationships = relationships,
            Metadata = metadata
        };
    }

    private static IReadOnlyList<NormalizedPlacedInstance> FilterPlacedInstances(
        IReadOnlyList<NormalizedPlacedInstance>? placedInstances,
        IReadOnlyList<NormalizedFamily> families)
    {
        if (placedInstances is not { Count: > 0 })
        {
            return [];
        }

        var familyNames = families
            .Select(family => family.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return placedInstances
            .Where(instance => familyNames.Contains(instance.FamilyName))
            .OrderBy(instance => instance.Identity.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildTargetSetId(string name, string? correlationId)
    {
        var slug = new string(name
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-");
        }

        return string.IsNullOrWhiteSpace(correlationId)
            ? $"target-set-{slug}"
            : $"target-set-{slug}-{correlationId}";
    }

    private static TargetSetContracts.FamilyTargetSetStatistics BuildStatistics(
        FamilyDiscoveryResult discoveryResult,
        SelectionContracts.FamilySelectionResult selectionResult,
        FilteringContracts.FamilyFilterResult filterResult,
        ImportedCadDetectionResult detectionResult,
        IReadOnlyList<NormalizedFamily> targetFamilies)
    {
        return new TargetSetContracts.FamilyTargetSetStatistics
        {
            DiscoveredFamilies = discoveryResult.Families?.Count ?? 0,
            SelectedFamilies = selectionResult.SelectedFamilies?.Count ?? 0,
            FilteredFamilies = filterResult.FilteredFamilies?.Count ?? 0,
            ComplianceCheckedFamilies = detectionResult.Findings?.Count ?? 0,
            TargetFamilies = targetFamilies.Count,
            ImportedCadReferencesFound = detectionResult.Statistics?.ImportedCadReferencesFound ?? 0
        };
    }

    private static IReadOnlyList<FamilyEngineDiagnostic> CollectDiagnostics(
        FamilyDiscoveryResult discoveryResult,
        SelectionContracts.FamilySelectionResult selectionResult,
        FilteringContracts.FamilyFilterResult filterResult,
        ImportedCadDetectionResult detectionResult,
        string generatorId)
    {
        var diagnostics = new List<FamilyEngineDiagnostic>();

        if (discoveryResult.Diagnostics is not null)
        {
            diagnostics.AddRange(discoveryResult.Diagnostics);
        }

        if (selectionResult.Diagnostics is not null)
        {
            diagnostics.AddRange(selectionResult.Diagnostics);
        }

        if (filterResult.Diagnostics is not null)
        {
            diagnostics.AddRange(filterResult.Diagnostics);
        }

        if (detectionResult.Diagnostics is not null)
        {
            diagnostics.AddRange(detectionResult.Diagnostics);
        }

        diagnostics.Add(new FamilyEngineDiagnostic
        {
            Code = "FamilyTargetSet.Completed",
            Message = $"Target set generator '{generatorId}' completed pipeline execution.",
            Severity = FamilyEngineDiagnosticSeverity.Information,
            Location = $"generator:{generatorId}"
        });

        return diagnostics;
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        TargetSetContracts.FamilyTargetSetRequest request,
        string targetSetId)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["generationOperation"] = "family-target-set",
            ["targetSetId"] = targetSetId,
            ["targetSetName"] = request.Definition.Name
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
