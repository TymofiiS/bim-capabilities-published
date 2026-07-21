using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Mapping;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Translators;

internal static class FamilyTranslator
{
    internal static NormalizedFamily? Translate(IRevitFamilyHandle? handle)
    {
        if (handle is null)
        {
            return null;
        }

        var familyTypes = handle.FamilyTypes
            .Select(FamilyTypeTranslator.Translate)
            .Where(familyType => familyType is not null)
            .Cast<NormalizedFamilyType>()
            .OrderBy(familyType => familyType.Identity.Id, StringComparer.Ordinal)
            .ToList();

        return new NormalizedFamily
        {
            Identity = NormalizedIdentifierFactory.Create(handle.Id, ObjectTranslationSourceKinds.Family),
            Name = handle.Name,
            Category = CategoryTranslator.Translate(handle.Category),
            FamilyTypes = familyTypes.Count > 0 ? familyTypes : null,
            Metadata = MetadataCopier.Copy(handle.Metadata),
            Parameters = ParameterTranslator.TranslateCollection(handle.Parameters),
            Relationships = RelationshipTranslator.TranslateCollection(handle.Relationships)
        };
    }
}
