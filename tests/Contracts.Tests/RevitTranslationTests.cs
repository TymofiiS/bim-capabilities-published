using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Tests;

public class RevitTranslationTests
{
    private static readonly JsonSerializerOptions JsonOptions = RevitTranslationSerialization.Options;

    [Fact]
    public void Revit_translation_contracts_are_data_only_types()
    {
        var translationTypes = typeof(NormalizedObject).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(NormalizedObject).Namespace);

        Assert.All(translationTypes, type =>
        {
            if (type == typeof(IObjectTranslator))
            {
                return;
            }

            if (type == typeof(NormalizedParameterStorageType) || type == typeof(NormalizedRelationshipType))
            {
                return;
            }

            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void NormalizedObject_can_be_constructed_with_required_properties()
    {
        var normalizedObject = RevitTranslationTestData.CreateDoorInstance();

        Assert.Equal("element-001", normalizedObject.Identity.Id);
        Assert.Equal("Door Instance 001", normalizedObject.Name);
        Assert.Equal("Doors", normalizedObject.Category!.Name);
        Assert.Equal("Level 1", normalizedObject.Metadata!["level"]);
        Assert.Single(normalizedObject.Relationships!);
        Assert.Single(normalizedObject.Parameters!);
    }

    [Fact]
    public void NormalizedFamily_can_be_constructed_with_family_types()
    {
        var family = RevitTranslationTestData.CreateDoorFamily();

        Assert.Equal("family-001", family.Identity.Id);
        Assert.Equal("HTL_Door_01", family.Name);
        Assert.Equal("Doors", family.Category!.Name);
        Assert.Equal(2, family.FamilyTypes!.Count);
        Assert.Equal("HTL_Door_01_900x2100", family.FamilyTypes[0].Name);
        Assert.Single(family.Relationships!);
        Assert.Single(family.Parameters!);
    }

    [Fact]
    public void NormalizedFamilyType_supports_parameters_and_metadata()
    {
        var familyType = RevitTranslationTestData.CreateDoorType("family-type-001", "HTL_Door_01_900x2100");

        Assert.Equal("family-type-001", familyType.Identity.Id);
        Assert.Equal("HTL_Door_01_900x2100", familyType.Name);
        Assert.Single(familyType.Parameters!);
    }

    [Fact]
    public void NormalizedCategory_supports_identity_name_and_metadata()
    {
        var category = RevitTranslationTestData.CreateDoorsCategory();

        Assert.Equal("category-doors", category.Identifier.Id);
        Assert.Equal("Doors", category.Name);
        Assert.Equal("OST_Doors", category.Metadata!["builtInCategory"]);
    }

    [Fact]
    public void NormalizedParameter_supports_storage_type_and_shared_flag()
    {
        var parameter = RevitTranslationTestData.CreateFireRatingParameter();

        Assert.Equal("FireRating", parameter.Name);
        Assert.Equal("60", parameter.Value);
        Assert.Equal(NormalizedParameterStorageType.String, parameter.StorageType);
        Assert.True(parameter.IsSharedParameter);
        Assert.Equal("f1a2b3c4-d5e6-7890-abcd-ef1234567890", parameter.Metadata!["guid"]);
    }

    [Fact]
    public void NormalizedRelationship_supports_object_relationships()
    {
        var family = RevitTranslationTestData.CreateDoorFamily();
        var relationship = family.Relationships![0];

        Assert.Equal("family-001", relationship.Source.Id);
        Assert.Equal("nested-family-001", relationship.Target.Id);
        Assert.Equal(NormalizedRelationshipType.Nested, relationship.RelationshipType);
        Assert.Equal("nestedFamily", relationship.Metadata!["referenceType"]);
    }

    [Fact]
    public void NormalizedIdentifier_supports_identity_scope_and_kind()
    {
        var identifier = RevitTranslationTestData.CreateDoorFamilyIdentifier();

        Assert.Equal("family-001", identifier.Id);
        Assert.Equal("family", identifier.Kind);
        Assert.Equal("project-document", identifier.Scope);
    }

    [Theory]
    [InlineData(NormalizedParameterStorageType.Integer)]
    [InlineData(NormalizedParameterStorageType.Double)]
    [InlineData(NormalizedParameterStorageType.String)]
    [InlineData(NormalizedParameterStorageType.ElementId)]
    [InlineData(NormalizedParameterStorageType.Boolean)]
    [InlineData(NormalizedParameterStorageType.None)]
    public void NormalizedParameterStorageType_supports_required_values(NormalizedParameterStorageType storageType)
    {
        var parameter = RevitTranslationTestData.CreateFireRatingParameter() with
        {
            StorageType = storageType
        };

        Assert.Equal(storageType, parameter.StorageType);
    }

    [Fact]
    public void NormalizedObject_supports_json_round_trip_serialization()
    {
        var original = RevitTranslationTestData.CreateDoorInstance();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<NormalizedObject>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Identity.Id, roundTrip.Identity.Id);
        Assert.Equal(original.Name, roundTrip.Name);
        Assert.Equal(original.Category!.Name, roundTrip.Category!.Name);
        Assert.Equal(original.Relationships!.Count, roundTrip.Relationships!.Count);
        Assert.Equal(original.Parameters![0].Name, roundTrip.Parameters![0].Name);
        Assert.Equal(original.Parameters[0].StorageType, roundTrip.Parameters[0].StorageType);
    }

    [Fact]
    public void NormalizedFamily_supports_json_round_trip_serialization()
    {
        var original = RevitTranslationTestData.CreateDoorFamily();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<NormalizedFamily>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Identity.Id, roundTrip.Identity.Id);
        Assert.Equal(original.Name, roundTrip.Name);
        Assert.Equal(original.FamilyTypes!.Count, roundTrip.FamilyTypes!.Count);
        Assert.Equal(original.Relationships!.Count, roundTrip.Relationships!.Count);
        Assert.True(roundTrip.Parameters![0].IsSharedParameter);
    }

    [Fact]
    public void Revit_translation_contracts_use_init_only_immutable_properties()
    {
        var contractTypes = typeof(NormalizedObject).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(NormalizedObject).Namespace)
            .Where(type => type.IsClass);

        Assert.All(contractTypes, type =>
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.NotNull(property.SetMethod);
                Assert.Contains(
                    property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers(),
                    modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
            }
        });
    }

    [Fact]
    public void Revit_translation_contracts_do_not_reference_revit_or_engine_assemblies()
    {
        var contractsAssembly = typeof(NormalizedObject).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Family", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Parameter", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Naming", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
