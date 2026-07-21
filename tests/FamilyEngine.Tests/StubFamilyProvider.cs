using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Engines.Family.Tests;

internal sealed class StubFamilyProvider : IFamilyProvider
{
    private readonly IReadOnlyList<NormalizedFamily> _families;

    internal StubFamilyProvider(params NormalizedFamily[] families)
    {
        _families = families;
    }

    public FamilyQueryResult Retrieve(FamilyQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var families = ResolveFamilies(query);

        return new FamilyQueryResult
        {
            Families = families,
            Diagnostics =
            [
                new FamilyQueryDiagnostic
                {
                    Code = "FamilyProvider.Stub",
                    Message = "Stub family provider returned deterministic discovery data.",
                    Severity = FamilyQueryDiagnosticSeverity.Information,
                    Location = "provider:stub"
                }
            ],
            Statistics = new FamilyQueryStatistics
            {
                TotalFamilies = _families.Count,
                RetrievedFamilies = families.Count,
                FilteredFamilies = _families.Count - families.Count,
                CountsByCategory = families
                    .GroupBy(family => family.Category?.Name ?? "unknown", StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal)
            },
            QueryMetadata = new FamilyQueryMetadata
            {
                CorrelationId = query.CorrelationId,
                ExecutedAt = new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero),
                ProviderId = "stub-family-provider",
                Properties = new Dictionary<string, string>
                {
                    ["implementation"] = "test-stub"
                }
            }
        };
    }

    private IReadOnlyList<NormalizedFamily> ResolveFamilies(FamilyQuery query)
    {
        IEnumerable<NormalizedFamily> candidates = _families;

        if (query.Categories is { Count: > 0 })
        {
            var categories = new HashSet<string>(query.Categories, StringComparer.OrdinalIgnoreCase);
            candidates = candidates.Where(family =>
                family.Category is not null && categories.Contains(family.Category.Name));
        }

        if (query.FamilyNames is { Count: > 0 })
        {
            var names = new HashSet<string>(query.FamilyNames, StringComparer.OrdinalIgnoreCase);
            candidates = candidates.Where(family => names.Contains(family.Name));
        }

        return candidates
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .ToArray();
    }

    internal static StubFamilyProvider CreateDefault()
    {
        return new StubFamilyProvider(
            CreateFamily(
                "family-001",
                "HTL_Door_01",
                "Doors",
                [
                    CreateFamilyType("family-type-001", "HTL_Door_01_900x2100",
                    [
                        CreateParameter("param-001", "FireRating", "60")
                    ]),
                    CreateFamilyType("family-type-002", "HTL_Door_01_1000x2100",
                    [
                        CreateParameter("param-002", "FireRating", "90")
                    ])
                ],
                parameters: [CreateParameter("param-003", "Width", "900")],
                relationships:
                [
                    CreateRelationship("family-001", "nested-family-001", NormalizedRelationshipType.Nested, "family")
                ]),
            CreateFamily(
                "family-002",
                "HTL_Window_01",
                "Windows",
                [
                    CreateFamilyType("family-type-003", "HTL_Window_01_1200x1200",
                    [
                        CreateParameter("param-004", "AcousticRating", "45")
                    ])
                ]));
    }

    internal static StubFamilyProvider CreateForTargetSetGeneration()
    {
        return new StubFamilyProvider(
            CreateFamily(
                "family-001",
                "HTL_Door_01",
                "Doors",
                [
                    CreateFamilyType("family-type-001", "HTL_Door_01_900x2100",
                    [
                        CreateParameter("param-001", "FireRating", "60")
                    ])
                ],
                parameters: [CreateParameter("param-003", "Width", "900")],
                relationships:
                [
                    CreateRelationship("family-001", "imported-cad-001", NormalizedRelationshipType.Reference, "importedCad", RelationshipType.ImportedCad),
                    CreateRelationship("family-001", "nested-family-001", NormalizedRelationshipType.Nested, "family", RelationshipType.NestedFamily)
                ]),
            CreateFamily(
                "family-002",
                "HTL_Window_01",
                "Windows",
                [
                    CreateFamilyType("family-type-003", "HTL_Window_01_1200x1200",
                    [
                        CreateParameter("param-004", "AcousticRating", "45")
                    ])
                ]),
            CreateFamily(
                "family-005",
                "HTL_Door_02",
                "Doors",
                [
                    CreateFamilyType("family-type-005", "HTL_Door_02_900x2100",
                    [
                        CreateParameter("param-005", "FireRating", "30")
                    ])
                ]));
    }

    internal static StubFamilyProvider CreateForFiltering()
    {
        return new StubFamilyProvider(
            CreateFamily(
                "family-001",
                "HTL_Door_01",
                "Doors",
                [
                    CreateFamilyType("family-type-001", "HTL_Door_01_900x2100",
                    [
                        CreateParameter("param-001", "FireRating", "60")
                    ])
                ],
                parameters: [CreateParameter("param-003", "Width", "900")],
                relationships:
                [
                    CreateRelationship("family-001", "nested-family-001", NormalizedRelationshipType.Nested, "family")
                ]),
            CreateFamily(
                "family-002",
                "HTL_Window_01",
                "Windows",
                [
                    CreateFamilyType("family-type-003", "HTL_Window_01_1200x1200",
                    [
                        CreateParameter("param-004", "GlazingType", "Double")
                    ])
                ]),
            CreateFamily(
                "family-003",
                "HTL_Empty_01",
                "Doors",
                []),
            CreateFamily(
                "family-004",
                "HTL_Unused_01",
                "Windows",
                [
                    CreateFamilyType("family-type-004", "HTL_Unused_01_900x900")
                ],
                metadata: new Dictionary<string, string>
                {
                    ["isUnused"] = "true"
                }));
    }

    private static NormalizedFamily CreateFamily(
        string id,
        string name,
        string categoryName,
        IReadOnlyList<NormalizedFamilyType> familyTypes,
        IReadOnlyList<NormalizedParameter>? parameters = null,
        IReadOnlyList<NormalizedRelationship>? relationships = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier
            {
                Id = id,
                Kind = "family",
                Scope = "project-document"
            },
            Name = name,
            Category = new NormalizedCategory
            {
                Identifier = new NormalizedIdentifier
                {
                    Id = $"category-{categoryName.ToLowerInvariant()}",
                    Kind = "category"
                },
                Name = categoryName
            },
            FamilyTypes = familyTypes,
            Parameters = parameters,
            Relationships = relationships,
            Metadata = metadata
        };
    }

    private static NormalizedFamilyType CreateFamilyType(
        string id,
        string name,
        IReadOnlyList<NormalizedParameter>? parameters = null)
    {
        return new NormalizedFamilyType
        {
            Identity = new NormalizedIdentifier
            {
                Id = id,
                Kind = "familyType"
            },
            Name = name,
            Parameters = parameters
        };
    }

    private static NormalizedParameter CreateParameter(string id, string name, string? value = null)
    {
        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier
            {
                Id = id,
                Kind = "parameter"
            },
            Name = name,
            Value = value,
            StorageType = NormalizedParameterStorageType.String
        };
    }

    private static NormalizedRelationship CreateRelationship(
        string sourceId,
        string targetId,
        NormalizedRelationshipType relationshipType,
        string targetKind,
        RelationshipType? queryRelationshipType = null)
    {
        var metadata = queryRelationshipType is null
            ? null
            : new Dictionary<string, string>
            {
                ["queryRelationshipType"] = queryRelationshipType.Value.ToString()
            };

        return new NormalizedRelationship
        {
            Source = new NormalizedIdentifier { Id = sourceId, Kind = "family" },
            Target = new NormalizedIdentifier { Id = targetId, Kind = targetKind },
            RelationshipType = relationshipType,
            Metadata = metadata
        };
    }
}
