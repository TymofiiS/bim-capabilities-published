using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Deterministic stub responses used by the Revit Adapter read skeleton.
/// </summary>
internal static class RevitReadStubResponses
{
    internal const string StubProviderId = "revit-adapter-read-skeleton";
    internal const string StubNotImplementedMessage = "Revit API retrieval is not implemented. Contract composition stub response returned.";

    internal static readonly DateTimeOffset StubExecutedAt = new(2026, 6, 19, 23, 30, 0, TimeSpan.Zero);

    internal static FamilyQueryResult CreateFamilyQueryResult(FamilyQuery query)
    {
        return new FamilyQueryResult
        {
            Families = [CreateDoorFamily()],
            Diagnostics =
            [
                new FamilyQueryDiagnostic
                {
                    Code = "FamilyProvider.NotImplemented",
                    Message = StubNotImplementedMessage,
                    Severity = FamilyQueryDiagnosticSeverity.Information,
                    Location = "provider:family"
                }
            ],
            Statistics = new FamilyQueryStatistics
            {
                TotalFamilies = 1,
                RetrievedFamilies = 1,
                FilteredFamilies = 0,
                CountsByCategory = new Dictionary<string, int> { ["Doors"] = 1 }
            },
            QueryMetadata = CreateFamilyMetadata(query.CorrelationId)
        };
    }

    internal static ParameterQueryResult CreateParameterQueryResult(ParameterQuery query)
    {
        return new ParameterQueryResult
        {
            Parameters =
            [
                CreateParameter("FireRating", "60", isShared: true),
                CreateParameter("AcousticRating", "45", isShared: true),
                CreateParameter("RoomName", "Lobby", isShared: false),
                CreateParameter("Manufacturer", "HTL Components", isShared: true)
            ],
            Diagnostics =
            [
                new ParameterQueryDiagnostic
                {
                    Code = "ParameterProvider.NotImplemented",
                    Message = StubNotImplementedMessage,
                    Severity = ParameterQueryDiagnosticSeverity.Information,
                    Location = "provider:parameter"
                }
            ],
            Statistics = new ParameterQueryStatistics
            {
                TotalParameters = 4,
                RetrievedParameters = 4,
                FilteredParameters = 0,
                MissingParameters = 0,
                CountsByParameterName = new Dictionary<string, int>
                {
                    ["FireRating"] = 1,
                    ["AcousticRating"] = 1,
                    ["RoomName"] = 1,
                    ["Manufacturer"] = 1
                }
            },
            QueryMetadata = CreateParameterMetadata(query.CorrelationId)
        };
    }

    internal static RelationshipQueryResult CreateRelationshipQueryResult(RelationshipQuery query)
    {
        return new RelationshipQueryResult
        {
            Relationships =
            [
                CreateRelationship("family-001", "nested-family-001", NormalizedRelationshipType.Nested, RelationshipType.NestedFamily),
                CreateRelationship("family-001", "imported-cad-001", NormalizedRelationshipType.Reference, RelationshipType.ImportedCad),
                CreateRelationship("family-001", "hardware-family-001", NormalizedRelationshipType.Reference, RelationshipType.Dependency),
                CreateRelationship("family-001", "family-type-001", NormalizedRelationshipType.TypeDefinition, RelationshipType.FamilyType)
            ],
            Diagnostics =
            [
                new RelationshipQueryDiagnostic
                {
                    Code = "RelationshipProvider.NotImplemented",
                    Message = StubNotImplementedMessage,
                    Severity = RelationshipQueryDiagnosticSeverity.Information,
                    Location = "provider:relationship"
                }
            ],
            Statistics = new RelationshipQueryStatistics
            {
                TotalRelationships = 4,
                RetrievedRelationships = 4,
                FilteredRelationships = 0,
                CountsByRelationshipType = new Dictionary<string, int>
                {
                    [RelationshipType.NestedFamily.ToString()] = 1,
                    [RelationshipType.ImportedCad.ToString()] = 1,
                    [RelationshipType.Dependency.ToString()] = 1,
                    [RelationshipType.FamilyType.ToString()] = 1
                }
            },
            QueryMetadata = CreateRelationshipMetadata(query.CorrelationId)
        };
    }

    internal static ObjectTranslationResult CreateObjectTranslationResult(ObjectTranslationQuery query)
    {
        return query.SourceKind switch
        {
            "family" => new ObjectTranslationResult { Family = CreateDoorFamily() },
            "element" => new ObjectTranslationResult { Object = CreateDoorInstance() },
            _ => new ObjectTranslationResult()
        };
    }

    internal static NormalizedFamily CreateDoorFamily()
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier
            {
                Id = "family-001",
                Kind = "family",
                Scope = "project-document"
            },
            Name = "HTL_Door_01",
            Category = CreateDoorsCategory(),
            FamilyTypes =
            [
                new NormalizedFamilyType
                {
                    Identity = new NormalizedIdentifier { Id = "family-type-001", Kind = "familyType" },
                    Name = "HTL_Door_01_900x2100",
                    Parameters = [CreateParameter("FireRating", "60", isShared: true)]
                }
            ],
            Parameters = [CreateParameter("FireRating", "60", isShared: true)]
        };
    }

    private static NormalizedObject CreateDoorInstance()
    {
        return new NormalizedObject
        {
            Identity = new NormalizedIdentifier
            {
                Id = "element-001",
                Kind = "element",
                Scope = "project-document"
            },
            Name = "Door Instance 001",
            Category = CreateDoorsCategory(),
            Parameters = [CreateParameter("FireRating", "60", isShared: true)]
        };
    }

    private static NormalizedCategory CreateDoorsCategory()
    {
        return new NormalizedCategory
        {
            Identifier = new NormalizedIdentifier { Id = "category-doors", Kind = "category" },
            Name = "Doors"
        };
    }

    private static NormalizedParameter CreateParameter(string name, string value, bool isShared)
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
            IsSharedParameter = isShared
        };
    }

    private static NormalizedRelationship CreateRelationship(
        string sourceId,
        string targetId,
        NormalizedRelationshipType normalizedType,
        RelationshipType queryType)
    {
        return new NormalizedRelationship
        {
            Source = new NormalizedIdentifier { Id = sourceId, Kind = "family" },
            Target = new NormalizedIdentifier { Id = targetId, Kind = "family" },
            RelationshipType = normalizedType,
            Metadata = new Dictionary<string, string>
            {
                ["queryRelationshipType"] = queryType.ToString()
            }
        };
    }

    private static FamilyQueryMetadata CreateFamilyMetadata(string? correlationId)
    {
        return new FamilyQueryMetadata
        {
            CorrelationId = correlationId,
            ExecutedAt = StubExecutedAt,
            ProviderId = StubProviderId,
            Properties = new Dictionary<string, string> { ["implementation"] = "skeleton" }
        };
    }

    private static ParameterQueryMetadata CreateParameterMetadata(string? correlationId)
    {
        return new ParameterQueryMetadata
        {
            CorrelationId = correlationId,
            ExecutedAt = StubExecutedAt,
            ProviderId = StubProviderId,
            Properties = new Dictionary<string, string> { ["implementation"] = "skeleton" }
        };
    }

    private static RelationshipQueryMetadata CreateRelationshipMetadata(string? correlationId)
    {
        return new RelationshipQueryMetadata
        {
            CorrelationId = correlationId,
            ExecutedAt = StubExecutedAt,
            ProviderId = StubProviderId,
            Properties = new Dictionary<string, string> { ["implementation"] = "skeleton" }
        };
    }
}
