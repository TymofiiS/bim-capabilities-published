using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Tests.Mocks;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class ParameterProviderTestFixtures
{
    internal const string CorrelationId = "corr-parameter-provider-001";

    internal const string FireRatingGuid = "f1a2b3c4-d5e6-7890-abcd-ef1234567890";

    internal static readonly DateTimeOffset FixedExecutedAt = new(2026, 6, 19, 23, 55, 0, TimeSpan.Zero);

    internal static RevitParameterProvider CreateProvider()
    {
        return new RevitParameterProvider(
            new MockRevitParameterCatalog(CreateSampleCatalog()),
            new FixedFamilyQueryClock(FixedExecutedAt));
    }

    internal static IReadOnlyList<IRevitParameterCatalogEntry> CreateSampleCatalog()
    {
        return
        [
            CreateEntry("parameter-fire-rating-door-900", "FireRating", "60", isShared: true, FireRatingGuid,
                "Doors", "family-door-001", "HTL_Door_01", "family-type-door-900", "HTL_Door_01_900x2100"),
            CreateEntry("parameter-acoustic-rating-door-900", "AcousticRating", "45", isShared: true, guid: null,
                "Doors", "family-door-001", "HTL_Door_01", "family-type-door-900", "HTL_Door_01_900x2100"),
            CreateEntry("parameter-room-name-door-900", "RoomName", "Lobby", isShared: false, guid: null,
                "Doors", "family-door-001", "HTL_Door_01", "family-type-door-900", "HTL_Door_01_900x2100",
                builtInParameter: "ROOM_NAME"),
            CreateEntry("parameter-manufacturer-door-900", "Manufacturer", "HTL Components", isShared: true, guid: null,
                "Doors", "family-door-001", "HTL_Door_01", "family-type-door-900", "HTL_Door_01_900x2100"),
            CreateEntry("parameter-acoustic-rating-window-1200", "AcousticRating", "40", isShared: true, guid: null,
                "Windows", "family-window-001", "HTL_Window_01", "family-type-window-1200", "HTL_Window_01_1200x1500"),
            CreateEntry("parameter-room-name-window-1200", "RoomName", "Office", isShared: false, guid: null,
                "Windows", "family-window-001", "HTL_Window_01", "family-type-window-1200", "HTL_Window_01_1200x1500",
                builtInParameter: "ROOM_NAME"),
            CreateEntry("parameter-manufacturer-window-1200", "Manufacturer", "HTL Components", isShared: true, guid: null,
                "Windows", "family-window-001", "HTL_Window_01", "family-type-window-1200", "HTL_Window_01_1200x1500")
        ];
    }

    internal static ParameterQuery CreateAllParametersQuery()
    {
        return new ParameterQuery
        {
            Scope = new ParameterQueryScope { Kind = ParameterQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static ParameterQuery CreateParameterByNameQuery(string parameterName)
    {
        return new ParameterQuery
        {
            ParameterNames = [parameterName],
            Scope = new ParameterQueryScope { Kind = ParameterQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static ParameterQuery CreateSharedParametersQuery()
    {
        return new ParameterQuery
        {
            SharedParameterNames = ["FireRating", "AcousticRating", "Manufacturer"],
            Scope = new ParameterQueryScope { Kind = ParameterQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static ParameterQuery CreateDoorCategoryQuery()
    {
        return new ParameterQuery
        {
            Categories = ["Doors"],
            Scope = new ParameterQueryScope { Kind = ParameterQueryScopeKind.EntireModel },
            CorrelationId = CorrelationId
        };
    }

    internal static ParameterQuery CreateFamilyScopeQuery()
    {
        return new ParameterQuery
        {
            Scope = new ParameterQueryScope
            {
                Kind = ParameterQueryScopeKind.SelectedFamilies,
                ScopeIdentifiers = ["family-door-001"]
            },
            CorrelationId = CorrelationId
        };
    }

    private static RevitParameterCatalogEntry CreateEntry(
        string id,
        string name,
        string value,
        bool isShared,
        string? guid,
        string categoryName,
        string familyId,
        string familyName,
        string familyTypeId,
        string familyTypeName,
        string? builtInParameter = null)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);

        if (guid is not null)
        {
            metadata["guid"] = guid;
            metadata["parameterKind"] = "shared";
        }
        else if (builtInParameter is not null)
        {
            metadata["builtInParameter"] = builtInParameter;
            metadata["parameterKind"] = "builtIn";
        }
        else if (isShared)
        {
            metadata["parameterKind"] = "shared";
        }
        else
        {
            metadata["parameterKind"] = "family";
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
                Metadata = metadata
            },
            CategoryName = categoryName,
            FamilyId = familyId,
            FamilyName = familyName,
            FamilyTypeId = familyTypeId,
            FamilyTypeName = familyTypeName
        };
    }
}
