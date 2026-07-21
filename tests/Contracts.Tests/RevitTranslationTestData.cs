using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Tests;

internal static class RevitTranslationTestData
{
    internal static NormalizedIdentifier CreateDoorFamilyIdentifier()
    {
        return new NormalizedIdentifier
        {
            Id = "family-001",
            Kind = "family",
            Scope = "project-document"
        };
    }

    internal static NormalizedCategory CreateDoorsCategory()
    {
        return new NormalizedCategory
        {
            Identifier = new NormalizedIdentifier
            {
                Id = "category-doors",
                Kind = "category"
            },
            Name = "Doors",
            Metadata = new Dictionary<string, string>
            {
                ["builtInCategory"] = "OST_Doors"
            }
        };
    }

    internal static NormalizedParameter CreateFireRatingParameter()
    {
        return new NormalizedParameter
        {
            Identifier = new NormalizedIdentifier
            {
                Id = "parameter-fire-rating",
                Kind = "parameter"
            },
            Name = "FireRating",
            Value = "60",
            StorageType = NormalizedParameterStorageType.String,
            IsSharedParameter = true,
            Metadata = new Dictionary<string, string>
            {
                ["guid"] = "f1a2b3c4-d5e6-7890-abcd-ef1234567890"
            }
        };
    }

    internal static NormalizedFamilyType CreateDoorType(string id, string name)
    {
        return new NormalizedFamilyType
        {
            Identity = new NormalizedIdentifier
            {
                Id = id,
                Kind = "familyType"
            },
            Name = name,
            Parameters = [CreateFireRatingParameter()]
        };
    }

    internal static NormalizedFamily CreateDoorFamily()
    {
        return new NormalizedFamily
        {
            Identity = CreateDoorFamilyIdentifier(),
            Name = "HTL_Door_01",
            Category = CreateDoorsCategory(),
            FamilyTypes =
            [
                CreateDoorType("family-type-001", "HTL_Door_01_900x2100"),
                CreateDoorType("family-type-002", "HTL_Door_01_1000x2100")
            ],
            Metadata = new Dictionary<string, string>
            {
                ["isInPlace"] = "false"
            },
            Relationships =
            [
                new NormalizedRelationship
                {
                    Source = CreateDoorFamilyIdentifier(),
                    Target = new NormalizedIdentifier
                    {
                        Id = "nested-family-001",
                        Kind = "family"
                    },
                    RelationshipType = NormalizedRelationshipType.Nested,
                    Metadata = new Dictionary<string, string>
                    {
                        ["referenceType"] = "nestedFamily"
                    }
                }
            ],
            Parameters = [CreateFireRatingParameter()]
        };
    }

    internal static NormalizedObject CreateDoorInstance()
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
            Metadata = new Dictionary<string, string>
            {
                ["level"] = "Level 1"
            },
            Relationships =
            [
                new NormalizedRelationship
                {
                    Source = new NormalizedIdentifier
                    {
                        Id = "element-001",
                        Kind = "element"
                    },
                    Target = new NormalizedIdentifier
                    {
                        Id = "family-type-001",
                        Kind = "familyType"
                    },
                    RelationshipType = NormalizedRelationshipType.TypeDefinition
                }
            ],
            Parameters = [CreateFireRatingParameter()]
        };
    }
}
