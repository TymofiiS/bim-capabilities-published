using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class RevitReadIntegrationFixtures
{
    internal const string CorrelationId = "corr-revit-read-integration-001";

    internal static FamilyQuery CreateSampleFamilyQuery()
    {
        return new FamilyQuery
        {
            Categories = ["Doors"],
            FamilyNames = ["HTL_Door_01"],
            FamilyTypeNames = ["HTL_Door_01_900x2100"],
            Scope = new FamilyQueryScope
            {
                Kind = FamilyQueryScopeKind.SelectedFamilies,
                ScopeIdentifiers = ["family-001"]
            },
            CorrelationId = CorrelationId,
            Metadata = new Dictionary<string, string>
            {
                ["ruleId"] = "STD-ARC-OPENINGS-V01"
            }
        };
    }

    internal static ParameterQuery CreateSampleParameterQuery()
    {
        return new ParameterQuery
        {
            ParameterNames = ["FireRating", "AcousticRating", "RoomName", "Manufacturer"],
            SharedParameterNames = ["FireRating", "Manufacturer"],
            Categories = ["Doors"],
            Scope = new ParameterQueryScope
            {
                Kind = ParameterQueryScopeKind.SelectedFamilyTypes,
                ScopeIdentifiers = ["family-type-001"]
            },
            CorrelationId = CorrelationId
        };
    }

    internal static RelationshipQuery CreateSampleRelationshipQuery()
    {
        return new RelationshipQuery
        {
            SourceObjects = ["family-001"],
            RelationshipTypes =
            [
                RelationshipType.NestedFamily,
                RelationshipType.ImportedCad,
                RelationshipType.Dependency
            ],
            Scope = new RelationshipQueryScope
            {
                Kind = RelationshipQueryScopeKind.SelectedFamilies,
                ScopeIdentifiers = ["family-001"]
            },
            CorrelationId = CorrelationId
        };
    }

    internal static ObjectTranslationQuery CreateSampleFamilyTranslationQuery()
    {
        return new ObjectTranslationQuery
        {
            SourceObjectId = "family-001",
            SourceKind = "family",
            CorrelationId = CorrelationId
        };
    }

    internal static ObjectTranslationQuery CreateSampleElementTranslationQuery()
    {
        return new ObjectTranslationQuery
        {
            SourceObjectId = "element-001",
            SourceKind = "element",
            CorrelationId = CorrelationId
        };
    }
}
