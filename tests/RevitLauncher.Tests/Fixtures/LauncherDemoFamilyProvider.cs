using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Launchers.Revit.Tests.Fixtures;

internal sealed class LauncherDemoFamilyProvider : IFamilyProvider
{
    internal static readonly DateTimeOffset FixedExecutedAt = new(2026, 6, 20, 15, 0, 0, TimeSpan.Zero);

    private readonly bool _passScenario;

    internal LauncherDemoFamilyProvider(bool passScenario = true)
    {
        _passScenario = passScenario;
    }

    public FamilyQueryResult Retrieve(FamilyQuery query)
    {
        var families = _passScenario
            ? CreatePassFamilies()
            : CreateFailFamilies();

        if (query.Categories is { Count: > 0 })
        {
            var categories = new HashSet<string>(query.Categories, StringComparer.OrdinalIgnoreCase);
            families = families.Where(family => categories.Contains(family.Category!.Name)).ToArray();
        }

        return new FamilyQueryResult
        {
            Families = families,
            QueryMetadata = new FamilyQueryMetadata
            {
                ExecutedAt = FixedExecutedAt,
                ProviderId = "launcher-test-provider"
            }
        };
    }

    private static IReadOnlyList<NormalizedFamily> CreatePassFamilies()
    {
        return
        [
            CreateFamily("family-door-pass-001", "DR_SingleDoor", "Doors",
            [
                CreateFamilyType("family-type-door-pass-001", "DR_SingleDoor900x2100",
                [
                    CreateSharedParameter("FireRating", "f1a2b3c4-d5e6-7890-abcd-ef1234567890", "60"),
                    CreateParameter("RoomName", "Lobby")
                ])
            ]),
            CreateFamily("family-window-pass-001", "WN_SingleWindow", "Windows",
            [
                CreateFamilyType("family-type-window-pass-001", "WN_SingleWindow1200x1500",
                [
                    CreateSharedParameter("AcousticRating", "a1b2c3d4-e5f6-7890-abcd-ef1234567891", "45"),
                    CreateParameter("RoomName", "Office")
                ])
            ])
        ];
    }

    private static IReadOnlyList<NormalizedFamily> CreateFailFamilies()
    {
        return
        [
            CreateFamily("family-door-fail-001", "HTL_Door_01", "Doors",
            [
                CreateFamilyType("family-type-door-fail-001", "HTL_Door_01_900x2100",
                [
                    CreateSharedParameter("FireRating", "f1a2b3c4-d5e6-7890-abcd-ef1234567890", "60")
                ])
            ])
        ];
    }

    private static NormalizedFamily CreateFamily(
        string id,
        string name,
        string categoryName,
        IReadOnlyList<NormalizedFamilyType> familyTypes)
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier { Id = id, Kind = "family" },
            Name = name,
            Category = new NormalizedCategory
            {
                Identifier = new NormalizedIdentifier { Id = $"category-{categoryName.ToLowerInvariant()}", Kind = "category" },
                Name = categoryName
            },
            FamilyTypes = familyTypes
        };
    }

    private static NormalizedFamilyType CreateFamilyType(
        string id,
        string name,
        IReadOnlyList<NormalizedParameter> parameters)
    {
        return new NormalizedFamilyType
        {
            Identity = new NormalizedIdentifier { Id = id, Kind = "familyType" },
            Name = name,
            Parameters = parameters
        };
    }

    private static NormalizedParameter CreateSharedParameter(string name, string guid, string value)
    {
        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier { Id = guid, Kind = "parameter" },
            Name = name,
            Value = value,
            StorageType = NormalizedParameterStorageType.String,
            IsSharedParameter = true,
            Metadata = new Dictionary<string, string> { ["sharedParameterGuid"] = guid }
        };
    }

    private static NormalizedParameter CreateParameter(string name, string value)
    {
        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier { Id = $"parameter-{name.ToLowerInvariant()}", Kind = "parameter" },
            Name = name,
            Value = value,
            StorageType = NormalizedParameterStorageType.String
        };
    }
}
