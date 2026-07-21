using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Translators;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation;

/// <summary>
/// Translates resolved Revit objects into normalized BIMCapabilities contracts.
/// </summary>
public sealed class RevitObjectTranslator : IObjectTranslator
{
    private readonly IRevitObjectResolver _resolver;

    public RevitObjectTranslator(IRevitObjectResolver resolver)
    {
        ArgumentGuard.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    public ObjectTranslationResult Translate(ObjectTranslationQuery query)
    {
        ArgumentGuard.ThrowIfNull(query);

        return query.SourceKind switch
        {
            ObjectTranslationSourceKinds.Family => CreateFamilyResult(query.SourceObjectId),
            ObjectTranslationSourceKinds.FamilyType => CreateFamilyTypeResult(query.SourceObjectId),
            ObjectTranslationSourceKinds.Category => CreateCategoryResult(query.SourceObjectId),
            ObjectTranslationSourceKinds.Parameter => CreateParameterResult(query.SourceObjectId),
            ObjectTranslationSourceKinds.Element => CreateElementResult(query.SourceObjectId),
            ObjectTranslationSourceKinds.Relationship => CreateRelationshipResult(query.SourceObjectId),
            _ => new ObjectTranslationResult()
        };
    }

    private ObjectTranslationResult CreateFamilyResult(string sourceObjectId)
    {
        var family = FamilyTranslator.Translate(_resolver.ResolveFamily(sourceObjectId));
        return family is null ? new ObjectTranslationResult() : new ObjectTranslationResult { Family = family };
    }

    private ObjectTranslationResult CreateFamilyTypeResult(string sourceObjectId)
    {
        var familyType = FamilyTypeTranslator.Translate(_resolver.ResolveFamilyType(sourceObjectId));
        return familyType is null
            ? new ObjectTranslationResult()
            : new ObjectTranslationResult { FamilyType = familyType };
    }

    private ObjectTranslationResult CreateCategoryResult(string sourceObjectId)
    {
        var category = CategoryTranslator.Translate(_resolver.ResolveCategory(sourceObjectId));
        return category is null
            ? new ObjectTranslationResult()
            : new ObjectTranslationResult { Category = category };
    }

    private ObjectTranslationResult CreateParameterResult(string sourceObjectId)
    {
        var parameter = ParameterTranslator.Translate(_resolver.ResolveParameter(sourceObjectId));
        return parameter is null
            ? new ObjectTranslationResult()
            : new ObjectTranslationResult { Parameter = parameter };
    }

    private ObjectTranslationResult CreateElementResult(string sourceObjectId)
    {
        var element = ElementTranslator.Translate(_resolver.ResolveElement(sourceObjectId));
        return element is null ? new ObjectTranslationResult() : new ObjectTranslationResult { Object = element };
    }

    private ObjectTranslationResult CreateRelationshipResult(string sourceObjectId)
    {
        var relationship = RelationshipTranslator.Translate(_resolver.ResolveRelationship(sourceObjectId));
        return relationship is null
            ? new ObjectTranslationResult()
            : new ObjectTranslationResult { Relationship = relationship };
    }
}
