using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Tests.Mocks;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class FamilyProviderTestFixtures
{
    internal const string CorrelationId = "corr-family-provider-001";

    internal static readonly DateTimeOffset FixedExecutedAt = new(2026, 6, 19, 23, 45, 0, TimeSpan.Zero);

    internal static RevitFamilyProvider CreateProvider(IReadOnlyList<IRevitFamilyHandle> families)
    {
        return new RevitFamilyProvider(
            new MockRevitFamilyCatalog(families),
            new FixedFamilyQueryClock(FixedExecutedAt));
    }

    internal static IReadOnlyList<IRevitFamilyHandle> CreateSampleCatalog()
    {
        var doorCategory = CreateCategory("category-doors", "Doors", "OST_Doors");
        var windowCategory = CreateCategory("category-windows", "Windows", "OST_Windows");
        var genericCategory = CreateCategory("category-generic", "Generic Models", "OST_GenericModel");

        var fireRatingParameter = new MockRevitParameterHandle
        {
            Id = "parameter-fire-rating",
            Name = "FireRating",
            Value = "60",
            StorageType = "String",
            IsSharedParameter = true
        };

        return
        [
            CreateFamily(
                "family-door-001",
                "HTL_Door_01",
                doorCategory,
                [
                    CreateFamilyType("family-type-door-900", "HTL_Door_01_900x2100", fireRatingParameter),
                    CreateFamilyType("family-type-door-1000", "HTL_Door_01_1000x2100", fireRatingParameter)
                ],
                fireRatingParameter),
            CreateFamily(
                "family-window-001",
                "HTL_Window_01",
                windowCategory,
                [
                    CreateFamilyType("family-type-window-1200", "HTL_Window_01_1200x1500", fireRatingParameter)
                ],
                fireRatingParameter),
            CreateFamily(
                "family-generic-001",
                "HTL_Generic_01",
                genericCategory,
                [
                    CreateFamilyType("family-type-generic-default", "HTL_Generic_01_Default", fireRatingParameter)
                ],
                fireRatingParameter)
        ];
    }

    internal static FamilyQuery CreateAllFamiliesQuery()
    {
        return new FamilyQuery
        {
            Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static FamilyQuery CreateDoorFamiliesQuery()
    {
        return new FamilyQuery
        {
            Categories = ["Doors"],
            Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static FamilyQuery CreateWindowFamiliesQuery()
    {
        return new FamilyQuery
        {
            Categories = ["Windows"],
            Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static FamilyQuery CreateFamilyByNameQuery(string familyName)
    {
        return new FamilyQuery
        {
            FamilyNames = [familyName],
            Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static FamilyQuery CreateFamilyTypesQuery(string familyTypeName)
    {
        return new FamilyQuery
        {
            FamilyTypeNames = [familyTypeName],
            Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    private static MockRevitCategoryHandle CreateCategory(string id, string name, string builtInCategory)
    {
        return new MockRevitCategoryHandle
        {
            Id = id,
            Name = name,
            Metadata = new Dictionary<string, string>
            {
                ["builtInCategory"] = builtInCategory
            }
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
            Metadata = new Dictionary<string, string>
            {
                ["isInPlace"] = "false"
            }
        };
    }
}
