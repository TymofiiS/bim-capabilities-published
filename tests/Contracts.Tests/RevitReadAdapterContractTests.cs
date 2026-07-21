using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Tests;

public class RevitReadAdapterContractTests
{
    [Fact]
    public void IRevitReadAdapter_exposes_required_read_services()
    {
        var properties = typeof(IRevitReadAdapter).GetProperties();

        Assert.Contains(properties, property => property.Name == nameof(IRevitReadAdapter.Families) && property.PropertyType == typeof(IFamilyProvider));
        Assert.Contains(properties, property => property.Name == nameof(IRevitReadAdapter.Parameters) && property.PropertyType == typeof(IParameterProvider));
        Assert.Contains(properties, property => property.Name == nameof(IRevitReadAdapter.Relationships) && property.PropertyType == typeof(IRelationshipProvider));
        Assert.Contains(properties, property => property.Name == nameof(IRevitReadAdapter.Translator) && property.PropertyType == typeof(IObjectTranslator));
    }

    [Fact]
    public void IObjectTranslator_defines_translation_contract_without_implementation()
    {
        var method = Assert.Single(typeof(IObjectTranslator).GetMethods(), candidate => candidate.Name == "Translate");

        Assert.Equal(typeof(ObjectTranslationResult), method.ReturnType);
        Assert.Equal(typeof(ObjectTranslationQuery), method.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void ObjectTranslationQuery_supports_json_round_trip_serialization()
    {
        var original = new ObjectTranslationQuery
        {
            SourceObjectId = "family-001",
            SourceKind = "family",
            CorrelationId = "corr-translation-001"
        };

        var json = JsonSerializer.Serialize(original, RevitTranslationSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<ObjectTranslationQuery>(json, RevitTranslationSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.SourceObjectId, roundTrip.SourceObjectId);
        Assert.Equal(original.SourceKind, roundTrip.SourceKind);
    }

    [Fact]
    public void ObjectTranslation_contracts_are_data_only_types()
    {
        foreach (var type in new[] { typeof(ObjectTranslationQuery), typeof(ObjectTranslationResult) })
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        }
    }
}
