using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Tests.Mocks;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures.EndToEnd;

internal static class RevitAdapterEndToEndFixtureBuilder
{
    internal const string CorrelationId = "corr-revit-adapter-e2e-001";

    internal const string DoorFamilyId = "family-door-001";

    internal const string WindowFamilyId = "family-window-001";

    internal static readonly DateTimeOffset FixedExecutedAt = new(2026, 6, 20, 1, 0, 0, TimeSpan.Zero);

    internal static RevitAdapter CreateAdapter(EndToEndFixtureKind kind = EndToEndFixtureKind.Standard)
    {
        var families = CreateFamilyCatalog(kind);
        var parameters = CreateParameterCatalog(kind);
        var relationships = CreateRelationshipCatalog(kind);
        var resolver = CreateObjectResolver(families);

        return RevitAdapterReadSupport.CreateOperationalReadLayer(
            new MockRevitFamilyCatalog(families),
            new MockRevitParameterCatalog(parameters),
            new MockRevitRelationshipCatalog(relationships),
            resolver,
            new FixedFamilyQueryClock(FixedExecutedAt));
    }

    internal static RevitAdapterReadContext CreateDoorScenarioContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "door-retrieval" },
            FamilyQuery = new FamilyQuery
            {
                Categories = ["Doors"],
                Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
                CorrelationId = CorrelationId
            },
            TranslationQueries =
            [
                new ObjectTranslationQuery
                {
                    SourceObjectId = DoorFamilyId,
                    SourceKind = "family",
                    CorrelationId = CorrelationId
                }
            ]
        };
    }

    internal static RevitAdapterReadContext CreateWindowScenarioContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "window-retrieval" },
            FamilyQuery = new FamilyQuery
            {
                Categories = ["Windows"],
                Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
                CorrelationId = CorrelationId
            },
            TranslationQueries =
            [
                new ObjectTranslationQuery
                {
                    SourceObjectId = WindowFamilyId,
                    SourceKind = "family",
                    CorrelationId = CorrelationId
                }
            ]
        };
    }

    internal static RevitAdapterReadContext CreateParameterScenarioContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "parameter-retrieval" },
            ParameterQuery = new ParameterQuery
            {
                ParameterNames = ["FireRating", "RoomName", "AcousticRating", "Manufacturer"],
                Categories = ["Doors"],
                Scope = new ParameterQueryScope { Kind = ParameterQueryScopeKind.EntireModel },
                CorrelationId = CorrelationId
            }
        };
    }

    internal static RevitAdapterReadContext CreateRelationshipScenarioContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "relationship-retrieval" },
            RelationshipQuery = new RelationshipQuery
            {
                SourceObjects = [DoorFamilyId],
                RelationshipTypes =
                [
                    RelationshipType.NestedFamily,
                    RelationshipType.ImportedCad,
                    RelationshipType.Dependency
                ],
                Scope = new RelationshipQueryScope
                {
                    Kind = RelationshipQueryScopeKind.SelectedFamilies,
                    ScopeIdentifiers = [DoorFamilyId]
                },
                CorrelationId = CorrelationId
            }
        };
    }

    internal static RevitAdapterReadContext CreateImportedCadScenarioContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "imported-cad-retrieval" },
            RelationshipQuery = new RelationshipQuery
            {
                SourceObjects = [DoorFamilyId],
                RelationshipTypes = [RelationshipType.ImportedCad],
                Scope = new RelationshipQueryScope { Kind = RelationshipQueryScopeKind.EntireModel },
                CorrelationId = CorrelationId
            }
        };
    }

    internal static RevitAdapterReadContext CreateNestedFamilyScenarioContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "nested-family-retrieval" },
            RelationshipQuery = new RelationshipQuery
            {
                SourceObjects = [DoorFamilyId],
                RelationshipTypes = [RelationshipType.NestedFamily],
                Scope = new RelationshipQueryScope
                {
                    Kind = RelationshipQueryScopeKind.SelectedFamilies,
                    ScopeIdentifiers = [DoorFamilyId]
                },
                CorrelationId = CorrelationId
            }
        };
    }

    internal static RevitAdapterReadContext CreateMixedFamilyScenarioContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "mixed-family-retrieval" },
            FamilyQuery = new FamilyQuery
            {
                Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
                CorrelationId = CorrelationId
            }
        };
    }

    internal static RevitAdapterReadContext CreateLargeDatasetScenarioContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "large-dataset-retrieval" },
            FamilyQuery = new FamilyQuery
            {
                Categories = ["Doors"],
                Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
                CorrelationId = CorrelationId
            },
            ParameterQuery = new ParameterQuery
            {
                ParameterNames = ["FireRating"],
                Categories = ["Doors"],
                Scope = new ParameterQueryScope { Kind = ParameterQueryScopeKind.EntireModel },
                CorrelationId = CorrelationId
            }
        };
    }

    internal static RevitAdapterReadContext CreateCompleteWorkflowContext()
    {
        return new RevitAdapterReadContext
        {
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string> { ["scenario"] = "complete-read-workflow" },
            FamilyQuery = CreateDoorScenarioContext().FamilyQuery,
            ParameterQuery = CreateParameterScenarioContext().ParameterQuery,
            RelationshipQuery = CreateRelationshipScenarioContext().RelationshipQuery,
            TranslationQueries = CreateDoorScenarioContext().TranslationQueries
        };
    }

    private static IReadOnlyList<IRevitFamilyHandle> CreateFamilyCatalog(EndToEndFixtureKind kind)
    {
        if (kind == EndToEndFixtureKind.Large)
        {
            return CreateLargeFamilyCatalog();
        }

        var doorCategory = CreateCategory("category-doors", "Doors");
        var windowCategory = CreateCategory("category-windows", "Windows");
        var fireRating = CreateParameter("parameter-fire-rating", "FireRating", "60", isShared: true);

        return
        [
            CreateFamily(
                DoorFamilyId,
                "HTL_Door_01",
                doorCategory,
                [
                    CreateFamilyType("family-type-door-900", "HTL_Door_01_900x2100", fireRating)
                ],
                fireRating),
            CreateFamily(
                WindowFamilyId,
                "HTL_Window_01",
                windowCategory,
                [
                    CreateFamilyType("family-type-window-1200", "HTL_Window_01_1200x1500", fireRating)
                ],
                fireRating)
        ];
    }

    private static IReadOnlyList<IRevitFamilyHandle> CreateLargeFamilyCatalog()
    {
        var doorCategory = CreateCategory("category-doors", "Doors");
        var fireRating = CreateParameter("parameter-fire-rating-template", "FireRating", "60", isShared: true);
        var families = new List<IRevitFamilyHandle>(LargeFixture.FamilyCount);

        for (var index = 0; index < LargeFixture.FamilyCount; index++)
        {
            var familyId = $"family-door-large-{index:D3}";
            families.Add(CreateFamily(
                familyId,
                $"HTL_Door_Large_{index:D3}",
                doorCategory,
                [CreateFamilyType($"family-type-{index:D3}", $"HTL_Door_Large_{index:D3}_Default", fireRating)],
                fireRating));
        }

        return families;
    }

    private static IReadOnlyList<IRevitParameterCatalogEntry> CreateParameterCatalog(EndToEndFixtureKind kind)
    {
        if (kind == EndToEndFixtureKind.Large)
        {
            return CreateLargeParameterCatalog();
        }

        return
        [
            CreateParameterEntry("parameter-fire-rating-door-900", "FireRating", "60", true,
                DoorFamilyId, "HTL_Door_01", "family-type-door-900", "Doors"),
            CreateParameterEntry("parameter-acoustic-rating-door-900", "AcousticRating", "45", true,
                DoorFamilyId, "HTL_Door_01", "family-type-door-900", "Doors"),
            CreateParameterEntry("parameter-room-name-door-900", "RoomName", "Lobby", false,
                DoorFamilyId, "HTL_Door_01", "family-type-door-900", "Doors", builtInParameter: "ROOM_NAME"),
            CreateParameterEntry("parameter-manufacturer-door-900", "Manufacturer", "HTL Components", true,
                DoorFamilyId, "HTL_Door_01", "family-type-door-900", "Doors"),
            CreateParameterEntry("parameter-acoustic-rating-window-1200", "AcousticRating", "40", true,
                WindowFamilyId, "HTL_Window_01", "family-type-window-1200", "Windows"),
            CreateParameterEntry("parameter-room-name-window-1200", "RoomName", "Office", false,
                WindowFamilyId, "HTL_Window_01", "family-type-window-1200", "Windows", builtInParameter: "ROOM_NAME"),
            CreateParameterEntry("parameter-manufacturer-window-1200", "Manufacturer", "HTL Components", true,
                WindowFamilyId, "HTL_Window_01", "family-type-window-1200", "Windows")
        ];
    }

    private static IReadOnlyList<IRevitParameterCatalogEntry> CreateLargeParameterCatalog()
    {
        var entries = new List<IRevitParameterCatalogEntry>(LargeFixture.FamilyCount);

        for (var index = 0; index < LargeFixture.FamilyCount; index++)
        {
            var familyId = $"family-door-large-{index:D3}";
            entries.Add(CreateParameterEntry(
                $"parameter-fire-rating-{index:D3}",
                "FireRating",
                "60",
                true,
                familyId,
                $"HTL_Door_Large_{index:D3}",
                $"family-type-{index:D3}",
                "Doors"));
        }

        return entries;
    }

    private static IReadOnlyList<IRevitRelationshipCatalogEntry> CreateRelationshipCatalog(EndToEndFixtureKind kind)
    {
        if (kind == EndToEndFixtureKind.Large)
        {
            return [];
        }

        return
        [
            RelationshipRetrievalSupport.CreateEntry(
                DoorFamilyId, "family", "nested-family-door-001", "family",
                NormalizedRelationshipType.Nested, RelationshipType.NestedFamily, "nestedFamily"),
            RelationshipRetrievalSupport.CreateEntry(
                DoorFamilyId, "family", "imported-cad-door-001", "importedCad",
                NormalizedRelationshipType.Reference, RelationshipType.ImportedCad, "importedCad"),
            RelationshipRetrievalSupport.CreateEntry(
                DoorFamilyId, "family", "hardware-family-001", "family",
                NormalizedRelationshipType.Reference, RelationshipType.Dependency, "familyDependency"),
            RelationshipRetrievalSupport.CreateEntry(
                DoorFamilyId, "family", "family-type-door-900", "familyType",
                NormalizedRelationshipType.TypeDefinition, RelationshipType.FamilyType, "familyType"),
            RelationshipRetrievalSupport.CreateEntry(
                "host-element-001", "element", DoorFamilyId, "family",
                NormalizedRelationshipType.Host, RelationshipType.Host, "host")
        ];
    }

    private static MockRevitObjectResolver CreateObjectResolver(IReadOnlyList<IRevitFamilyHandle> families)
    {
        var resolver = new MockRevitObjectResolver();

        foreach (var family in families)
        {
            resolver.RegisterFamily(family.Id, family);
        }

        return resolver;
    }

    private static MockRevitCategoryHandle CreateCategory(string id, string name)
    {
        return new MockRevitCategoryHandle { Id = id, Name = name };
    }

    private static MockRevitParameterHandle CreateParameter(
        string id,
        string name,
        string value,
        bool isShared)
    {
        return new MockRevitParameterHandle
        {
            Id = id,
            Name = name,
            Value = value,
            StorageType = "String",
            IsSharedParameter = isShared
        };
    }

    private static MockRevitFamilyTypeHandle CreateFamilyType(
        string id,
        string name,
        MockRevitParameterHandle parameter)
    {
        return new MockRevitFamilyTypeHandle
        {
            Id = id,
            Name = name,
            Parameters = [parameter]
        };
    }

    private static MockRevitFamilyHandle CreateFamily(
        string id,
        string name,
        MockRevitCategoryHandle category,
        IReadOnlyList<IRevitFamilyTypeHandle> familyTypes,
        MockRevitParameterHandle parameter)
    {
        return new MockRevitFamilyHandle
        {
            Id = id,
            Name = name,
            Category = category,
            FamilyTypes = familyTypes,
            Parameters = [parameter],
            Metadata = new Dictionary<string, string> { ["isInPlace"] = "false" }
        };
    }

    private static RevitParameterCatalogEntry CreateParameterEntry(
        string id,
        string name,
        string value,
        bool isShared,
        string familyId,
        string familyName,
        string familyTypeId,
        string categoryName,
        string? builtInParameter = null)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);

        if (builtInParameter is not null)
        {
            metadata["builtInParameter"] = builtInParameter;
            metadata["parameterKind"] = "builtIn";
        }
        else if (isShared)
        {
            metadata["parameterKind"] = "shared";
        }

        return new RevitParameterCatalogEntry
        {
            Parameter = new MockRevitParameterHandle
            {
                Id = id,
                Name = name,
                Value = value,
                StorageType = "String",
                IsSharedParameter = isShared,
                Metadata = metadata.Count > 0 ? metadata : null
            },
            CategoryName = categoryName,
            FamilyId = familyId,
            FamilyName = familyName,
            FamilyTypeId = familyTypeId,
            FamilyTypeName = familyName
        };
    }
}

internal enum EndToEndFixtureKind
{
    Standard,
    Large
}
