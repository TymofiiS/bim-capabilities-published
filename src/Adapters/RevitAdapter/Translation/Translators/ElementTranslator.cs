using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Mapping;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Translators;

internal static class ElementTranslator
{
    internal static NormalizedObject? Translate(IRevitElementHandle? handle)
    {
        if (handle is null)
        {
            return null;
        }

        return new NormalizedObject
        {
            Identity = NormalizedIdentifierFactory.Create(handle.Id, ObjectTranslationSourceKinds.Element),
            Name = handle.Name,
            Category = CategoryTranslator.Translate(handle.Category),
            Metadata = MetadataCopier.Copy(handle.Metadata),
            Parameters = ParameterTranslator.TranslateCollection(handle.Parameters),
            Relationships = RelationshipTranslator.TranslateCollection(handle.Relationships)
        };
    }
}
