using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Tests;

internal static class ParameterRetrievalTestData
{
    internal const string SharedParameterFilePath = @"C:\Company\Standards\HTL_SharedParameters.txt";

    internal static readonly string[] MvpDoorParameterNames =
    [
        "FireRating",
        "AcousticRating",
        "RoomName",
        "Manufacturer"
    ];

    internal static readonly string[] MvpWindowParameterNames =
    [
        "AcousticRating",
        "RoomName",
        "Manufacturer"
    ];

    internal static ParameterQuery CreateMvpDoorParameterQuery()
    {
        return new ParameterQuery
        {
            ParameterNames = MvpDoorParameterNames,
            SharedParameterNames = ["FireRating", "AcousticRating", "Manufacturer"],
            SharedParameterGuids = ["f1a2b3c4-d5e6-7890-abcd-ef1234567890"],
            BuiltInParameterNames = ["ROOM_NAME"],
            Categories = ["Doors"],
            Scope = new ParameterQueryScope
            {
                Kind = ParameterQueryScopeKind.SelectedFamilies,
                ScopeIdentifiers = ["family-001"],
                Metadata = new Dictionary<string, string>
                {
                    ["documentType"] = "project"
                }
            },
            ObjectScope = new ParameterObjectScope
            {
                FamilyIdentifiers = ["family-001"],
                FamilyTypeIdentifiers = ["family-type-001"],
                ObjectKind = "familyType"
            },
            Filter = CreateMvpDoorParameterFilter(),
            SharedParameterFile = CreateSharedParameterFileReference(),
            Metadata = new Dictionary<string, string>
            {
                ["ruleId"] = "STD-ARC-OPENINGS-V01",
                ["queryPurpose"] = "parameter-validation"
            },
            CorrelationId = "corr-parameter-query-001"
        };
    }

    internal static ParameterQueryFilter CreateMvpDoorParameterFilter()
    {
        return new ParameterQueryFilter
        {
            ParameterName = new ParameterNameFilter
            {
                ExactNames = ["FireRating", "Manufacturer"],
                NamePattern = "*Rating"
            },
            SharedParameter = new SharedParameterFilter
            {
                SharedParameterNames = ["FireRating", "AcousticRating"],
                SharedParameterGuids = ["f1a2b3c4-d5e6-7890-abcd-ef1234567890"],
                SharedParameterFilePath = SharedParameterFilePath,
                MustExist = true
            },
            Value = new ParameterValueFilter
            {
                MustHaveValue = true
            },
            Category = new ParameterCategoryFilter
            {
                CategoryNames = ["Doors"],
                CategoryIdentifiers = ["category-doors"]
            },
            Object = new ParameterObjectFilter
            {
                ObjectIdentifiers = ["family-type-001"],
                ObjectKind = "familyType"
            }
        };
    }

    internal static ParameterSharedParameterFileReference CreateSharedParameterFileReference()
    {
        return new ParameterSharedParameterFileReference
        {
            FilePath = SharedParameterFilePath,
            FileVersion = "2026.1",
            Metadata = new Dictionary<string, string>
            {
                ["owner"] = "office-standards"
            }
        };
    }

    internal static ParameterQueryResult CreateMvpDoorParameterQueryResult()
    {
        return new ParameterQueryResult
        {
            Parameters =
            [
                CreateMvpParameter("FireRating", "60", isShared: true),
                CreateMvpParameter("AcousticRating", "45", isShared: true),
                CreateMvpParameter("RoomName", "Lobby", isShared: false),
                CreateMvpParameter("Manufacturer", "HTL Components", isShared: true)
            ],
            Diagnostics =
            [
                new ParameterQueryDiagnostic
                {
                    Code = "ParameterQuery.Information",
                    Message = "Retrieved MVP door parameters for selected families.",
                    Severity = ParameterQueryDiagnosticSeverity.Information,
                    Location = "scope:selectedFamilies"
                }
            ],
            Statistics = new ParameterQueryStatistics
            {
                TotalParameters = 8,
                RetrievedParameters = 4,
                FilteredParameters = 4,
                MissingParameters = 0,
                CountsByParameterName = new Dictionary<string, int>
                {
                    ["FireRating"] = 1,
                    ["AcousticRating"] = 1,
                    ["RoomName"] = 1,
                    ["Manufacturer"] = 1
                }
            },
            QueryMetadata = new ParameterQueryMetadata
            {
                CorrelationId = "corr-parameter-query-001",
                ExecutedAt = new DateTimeOffset(2026, 6, 19, 22, 30, 0, TimeSpan.Zero),
                ProviderId = "revit-adapter-read-layer",
                Properties = new Dictionary<string, string>
                {
                    ["scopeKind"] = "selectedFamilies",
                    ["sharedParameterFile"] = SharedParameterFilePath
                }
            }
        };
    }

    internal static NormalizedParameter CreateMvpParameter(string name, string value, bool isShared)
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
            StorageType = NormalizedParameterStorageType.String,
            IsSharedParameter = isShared,
            Metadata = isShared
                ? new Dictionary<string, string>
                {
                    ["sharedParameterFile"] = SharedParameterFilePath
                }
                : null
        };
    }
}
