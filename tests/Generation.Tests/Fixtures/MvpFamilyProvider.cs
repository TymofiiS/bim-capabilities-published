using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Generation.Tests.Fixtures;

internal enum MvpValidationScenario
{
    DemoPass
}

internal sealed class MvpFamilyProvider : IFamilyProvider
{
    private const string FireRatingGuid = "f1a2b3c4-d5e6-7890-abcd-ef1234567890";
    private const string AcousticRatingGuid = "a1b2c3d4-e5f6-7890-abcd-ef1234567891";

    internal static readonly DateTimeOffset FixedExecutedAt = new(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);

    private readonly IReadOnlyList<NormalizedFamily> _families;

    internal MvpFamilyProvider(MvpValidationScenario scenario)
    {
        _families = scenario switch
        {
            MvpValidationScenario.DemoPass => [CreateCompliantDoor(), CreateCompliantWindow()],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }

    public FamilyQueryResult Retrieve(FamilyQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<NormalizedFamily> candidates = _families;
        if (query.Categories is { Count: > 0 })
        {
            var categories = new HashSet<string>(query.Categories, StringComparer.OrdinalIgnoreCase);
            candidates = candidates.Where(family =>
                family.Category is not null && categories.Contains(family.Category.Name));
        }

        var families = candidates.OrderBy(family => family.Identity.Id, StringComparer.Ordinal).ToArray();

        return new FamilyQueryResult
        {
            Families = families,
            Diagnostics =
            [
                new FamilyQueryDiagnostic
                {
                    Code = "FamilyProvider.MvpFixture",
                    Message = "MVP-003 generated rule fixture provider returned deterministic discovery data.",
                    Severity = FamilyQueryDiagnosticSeverity.Information,
                    Location = "provider:mvp-003-fixture"
                }
            ],
            Statistics = new FamilyQueryStatistics
            {
                TotalFamilies = _families.Count,
                RetrievedFamilies = families.Length,
                FilteredFamilies = _families.Count - families.Length
            },
            QueryMetadata = new FamilyQueryMetadata
            {
                CorrelationId = query.CorrelationId,
                ExecutedAt = FixedExecutedAt,
                ProviderId = "mvp-003-family-provider"
            }
        };
    }

    private static NormalizedFamily CreateCompliantDoor()
    {
        return CreateFamily(
            "family-door-pass-001",
            "DR_SingleDoor",
            "Doors",
            [
                CreateFamilyType(
                    "family-type-door-pass-001",
                    "DR_SingleDoor900x2100",
                    [
                        CreateSharedParameter("FireRating", FireRatingGuid, "60"),
                        CreateParameter("RoomName", "Lobby")
                    ])
            ]);
    }

    private static NormalizedFamily CreateCompliantWindow()
    {
        return CreateFamily(
            "family-window-pass-001",
            "WN_SingleWindow",
            "Windows",
            [
                CreateFamilyType(
                    "family-type-window-pass-001",
                    "WN_SingleWindow1200x1500",
                    [
                        CreateSharedParameter("AcousticRating", AcousticRatingGuid, "45"),
                        CreateParameter("RoomName", "Office")
                    ])
            ]);
    }

    private static NormalizedFamily CreateFamily(
        string id,
        string name,
        string categoryName,
        IReadOnlyList<NormalizedFamilyType> familyTypes)
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier { Id = id, Kind = "family", Scope = "project-document" },
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

    private static NormalizedParameter CreateSharedParameter(string name, string guid, string? value = null)
    {
        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier { Id = guid, Kind = "parameter" },
            Name = name,
            Value = value,
            StorageType = NormalizedParameterStorageType.String,
            IsSharedParameter = true,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["sharedParameterGuid"] = guid
            }
        };
    }

    private static NormalizedParameter CreateParameter(string name, string? value = null)
    {
        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier
            {
                Id = $"parameter-{name.ToLowerInvariant()}",
                Kind = "parameter"
            },
            Name = name,
            Value = value,
            StorageType = NormalizedParameterStorageType.String
        };
    }
}
