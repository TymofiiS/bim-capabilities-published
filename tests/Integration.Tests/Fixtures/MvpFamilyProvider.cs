using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Integration.Tests.Fixtures;

internal enum MvpValidationScenario
{
    DoorPass,
    DoorFail,
    WindowPass,
    WindowFail,
    ImportedCadFail,
    FurniturePass,
    DemoPass
}

internal sealed class MvpFamilyProvider : IFamilyProvider
{
    private const string FireRatingGuid = "f1a2b3c4-d5e6-7890-abcd-ef1234567890";
    private const string AcousticRatingGuid = "a1b2c3d4-e5f6-7890-abcd-ef1234567891";
    private const string ManufacturerGuid = "c3d4e5f6-a7b8-9012-cdef-123456789013";

    internal static readonly DateTimeOffset FixedExecutedAt = new(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);

    private readonly IReadOnlyList<NormalizedFamily> _families;

    internal MvpFamilyProvider(MvpValidationScenario scenario)
    {
        _families = CreateFamilies(scenario);
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
                    Code = "FamilyProvider.MvpFixture",
                    Message = "MVP validation fixture provider returned deterministic discovery data.",
                    Severity = FamilyQueryDiagnosticSeverity.Information,
                    Location = "provider:mvp-fixture"
                }
            ],
            Statistics = new FamilyQueryStatistics
            {
                TotalFamilies = _families.Count,
                RetrievedFamilies = families.Count,
                FilteredFamilies = _families.Count - families.Count
            },
            QueryMetadata = new FamilyQueryMetadata
            {
                CorrelationId = query.CorrelationId,
                ExecutedAt = FixedExecutedAt,
                ProviderId = "mvp-family-provider",
                Properties = new Dictionary<string, string>
                {
                    ["implementation"] = "mvp-validation-fixture"
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

        return candidates
            .OrderBy(family => family.Identity.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<NormalizedFamily> CreateFamilies(MvpValidationScenario scenario)
    {
        return scenario switch
        {
            MvpValidationScenario.DoorPass => [CreateCompliantDoor()],
            MvpValidationScenario.DoorFail => [CreateNonCompliantDoor()],
            MvpValidationScenario.WindowPass => [CreateCompliantWindow()],
            MvpValidationScenario.WindowFail => [CreateNonCompliantWindow()],
            MvpValidationScenario.ImportedCadFail => [CreateDoorWithImportedCad()],
            MvpValidationScenario.FurniturePass => [CreateCompliantFurniture()],
            MvpValidationScenario.DemoPass => [CreateCompliantDoor(), CreateCompliantWindow()],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
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

    private static NormalizedFamily CreateNonCompliantDoor()
    {
        return CreateFamily(
            "family-door-fail-001",
            "HTL_Door_01",
            "Doors",
            [
                CreateFamilyType(
                    "family-type-door-fail-001",
                    "HTL_Door_01_900x2100",
                    [
                        CreateSharedParameter("FireRating", FireRatingGuid, "60")
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

    private static NormalizedFamily CreateCompliantFurniture()
    {
        return CreateFamily(
            "family-furniture-pass-001",
            "FR_Workstation",
            "Furniture",
            [
                CreateFamilyType(
                    "family-type-furniture-pass-001",
                    "FR_Workstation1200",
                    [
                        CreateSharedParameter("Manufacturer", ManufacturerGuid, "HTL Components")
                    ])
            ]);
    }

    private static NormalizedFamily CreateNonCompliantWindow()
    {
        return CreateFamily(
            "family-window-fail-001",
            "HTL_Window_01",
            "Windows",
            [
                CreateFamilyType(
                    "family-type-window-fail-001",
                    "HTL_Window_01_1200x1500",
                    [
                        CreateSharedParameter("AcousticRating", AcousticRatingGuid, "45")
                    ])
            ]);
    }

    private static NormalizedFamily CreateDoorWithImportedCad()
    {
        return CreateFamily(
            "family-door-cad-fail-001",
            "DR_DoorWithCad",
            "Doors",
            [
                CreateFamilyType(
                    "family-type-door-cad-fail-001",
                    "DR_DoorWithCad900x2100",
                    [
                        CreateSharedParameter("FireRating", FireRatingGuid, "60"),
                        CreateParameter("RoomName", "Lobby")
                    ])
            ],
            relationships:
            [
                CreateImportedCadRelationship("family-door-cad-fail-001", "imported-cad-001")
            ]);
    }

    private static NormalizedFamily CreateFamily(
        string id,
        string name,
        string categoryName,
        IReadOnlyList<NormalizedFamilyType> familyTypes,
        IReadOnlyList<NormalizedRelationship>? relationships = null)
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
            Relationships = relationships
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

    private static NormalizedRelationship CreateImportedCadRelationship(string sourceId, string targetId)
    {
        return new NormalizedRelationship
        {
            Source = new NormalizedIdentifier { Id = sourceId, Kind = "family" },
            Target = new NormalizedIdentifier { Id = targetId, Kind = "importedCad" },
            RelationshipType = NormalizedRelationshipType.Reference,
            Metadata = new Dictionary<string, string>
            {
                ["queryRelationshipType"] = RelationshipType.ImportedCad.ToString()
            }
        };
    }
}
