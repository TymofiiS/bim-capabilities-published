using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Mapping;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Translators;

internal static class FamilyTypeTranslator
{
    internal static NormalizedFamilyType? Translate(IRevitFamilyTypeHandle? handle)
    {
        if (handle is null)
        {
            return null;
        }

        return new NormalizedFamilyType
        {
            Identity = NormalizedIdentifierFactory.Create(handle.Id, ObjectTranslationSourceKinds.FamilyType),
            Name = handle.Name,
            Metadata = MetadataCopier.Copy(handle.Metadata),
            Parameters = ParameterTranslator.TranslateCollection(handle.Parameters)
        };
    }
}
