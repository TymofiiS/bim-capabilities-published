using BIMCapabilities.Adapters.Revit.Tests.Mocks;
using BIMCapabilities.Adapters.Revit.Translation;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class ObjectTranslatorTests
{
    [Fact]
    public void Family_translation_populates_normalized_contract()
    {
        var resolver = CreateDoorFamilyResolver();
        var translator = new RevitObjectTranslator(resolver);

        var result = translator.Translate(new ObjectTranslationQuery
        {
            SourceObjectId = "family-001",
            SourceKind = "family"
        });

        Assert.NotNull(result.Family);
        Assert.Equal("family-001", result.Family!.Identity.Id);
        Assert.Equal("family", result.Family.Identity.Kind);
        Assert.Equal("HTL_Door_01", result.Family.Name);
        Assert.Equal("Doors", result.Family.Category!.Name);
        Assert.Equal(2, result.Family.FamilyTypes!.Count);
        Assert.Equal("HTL_Door_01_900x2100", result.Family.FamilyTypes[0].Name);
        Assert.Single(result.Family.Parameters!);
        Assert.Equal("FireRating", result.Family.Parameters![0].Name);
    }

    [Fact]
    public void FamilyType_translation_populates_normalized_contract()
    {
        var resolver = CreateDoorFamilyResolver();
        resolver.RegisterFamilyType(
            "family-type-001",
            new MockRevitFamilyTypeHandle
            {
                Id = "family-type-001",
                Name = "HTL_Door_01_900x2100",
                Parameters =
                [
                    new MockRevitParameterHandle
                    {
                        Id = "parameter-fire-rating",
                        Name = "FireRating",
                        Value = "60",
                        StorageType = "String",
                        IsSharedParameter = true
                    }
                ]
            });

        var translator = new RevitObjectTranslator(resolver);

        var result = translator.Translate(new ObjectTranslationQuery
        {
            SourceObjectId = "family-type-001",
            SourceKind = "familyType"
        });

        Assert.NotNull(result.FamilyType);
        Assert.Equal("family-type-001", result.FamilyType!.Identity.Id);
        Assert.Equal("familyType", result.FamilyType.Identity.Kind);
        Assert.Equal("HTL_Door_01_900x2100", result.FamilyType.Name);
        Assert.Single(result.FamilyType.Parameters!);
    }

    [Fact]
    public void Category_translation_populates_normalized_contract()
    {
        var resolver = CreateDoorFamilyResolver();
        var translator = new RevitObjectTranslator(resolver);

        var result = translator.Translate(new ObjectTranslationQuery
        {
            SourceObjectId = "category-doors",
            SourceKind = "category"
        });

        Assert.NotNull(result.Category);
        Assert.Equal("category-doors", result.Category!.Identifier.Id);
        Assert.Equal("Doors", result.Category.Name);
        Assert.Equal("OST_Doors", result.Category.Metadata!["builtInCategory"]);
    }

    [Fact]
    public void Parameter_translation_populates_normalized_contract()
    {
        var resolver = CreateDoorFamilyResolver();
        var translator = new RevitObjectTranslator(resolver);

        var result = translator.Translate(new ObjectTranslationQuery
        {
            SourceObjectId = "parameter-fire-rating",
            SourceKind = "parameter"
        });

        Assert.NotNull(result.Parameter);
        Assert.Equal("FireRating", result.Parameter!.Name);
        Assert.Equal("60", result.Parameter.Value);
        Assert.Equal(NormalizedParameterStorageType.String, result.Parameter.StorageType);
        Assert.True(result.Parameter.IsSharedParameter);
        Assert.Equal("f1a2b3c4-d5e6-7890-abcd-ef1234567890", result.Parameter.Metadata!["guid"]);
    }

    [Fact]
    public void Null_resolution_returns_empty_translation_result()
    {
        var translator = new RevitObjectTranslator(new MockRevitObjectResolver());

        var result = translator.Translate(new ObjectTranslationQuery
        {
            SourceObjectId = "missing-family",
            SourceKind = "family"
        });

        Assert.Null(result.Family);
        Assert.Null(result.Object);
        Assert.Null(result.Category);
        Assert.Null(result.Parameter);
    }

    [Fact]
    public void Unknown_source_kind_returns_empty_translation_result()
    {
        var translator = new RevitObjectTranslator(CreateDoorFamilyResolver());

        var result = translator.Translate(new ObjectTranslationQuery
        {
            SourceObjectId = "family-001",
            SourceKind = "unsupported-kind"
        });

        Assert.Null(result.Family);
        Assert.Null(result.Object);
    }

    [Fact]
    public void Translation_output_is_deterministic_for_same_input()
    {
        var resolver = CreateDoorFamilyResolver();
        var translator = new RevitObjectTranslator(resolver);
        var query = new ObjectTranslationQuery
        {
            SourceObjectId = "family-001",
            SourceKind = "family"
        };

        var first = translator.Translate(query);
        var second = translator.Translate(query);

        Assert.Equal(first.Family!.Identity.Id, second.Family!.Identity.Id);
        Assert.Equal(first.Family.Name, second.Family.Name);
        Assert.Equal(first.Family.FamilyTypes!.Count, second.Family.FamilyTypes!.Count);
        Assert.Equal(
            first.Family.FamilyTypes.Select(type => type.Identity.Id),
            second.Family.FamilyTypes.Select(type => type.Identity.Id));
        Assert.Equal(
            first.Family.Parameters!.Select(parameter => parameter.Identifier.Id),
            second.Family.Parameters!.Select(parameter => parameter.Identifier.Id));
    }

    [Fact]
    public void Translate_throws_when_query_is_null()
    {
        var translator = new RevitObjectTranslator(new MockRevitObjectResolver());

        Assert.Throws<ArgumentNullException>(() => translator.Translate(null!));
    }

    private static MockRevitObjectResolver CreateDoorFamilyResolver()
    {
        var fireRatingParameter = new MockRevitParameterHandle
        {
            Id = "parameter-fire-rating",
            Name = "FireRating",
            Value = "60",
            StorageType = "String",
            IsSharedParameter = true,
            Metadata = new Dictionary<string, string>
            {
                ["guid"] = "f1a2b3c4-d5e6-7890-abcd-ef1234567890"
            }
        };

        var category = new MockRevitCategoryHandle
        {
            Id = "category-doors",
            Name = "Doors",
            Metadata = new Dictionary<string, string>
            {
                ["builtInCategory"] = "OST_Doors"
            }
        };

        var familyTypes = new List<IRevitFamilyTypeHandle>
        {
            new MockRevitFamilyTypeHandle
            {
                Id = "family-type-002",
                Name = "HTL_Door_01_1000x2100",
                Parameters = [fireRatingParameter]
            },
            new MockRevitFamilyTypeHandle
            {
                Id = "family-type-001",
                Name = "HTL_Door_01_900x2100",
                Parameters = [fireRatingParameter]
            }
        };

        var family = new MockRevitFamilyHandle
        {
            Id = "family-001",
            Name = "HTL_Door_01",
            Category = category,
            FamilyTypes = familyTypes,
            Parameters = [fireRatingParameter],
            Metadata = new Dictionary<string, string>
            {
                ["isInPlace"] = "false"
            }
        };

        var resolver = new MockRevitObjectResolver();
        resolver.RegisterFamily("family-001", family);
        resolver.RegisterCategory("category-doors", category);
        resolver.RegisterParameter("parameter-fire-rating", fireRatingParameter);
        return resolver;
    }
}
