using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Mapping;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Translators;

internal static class CategoryTranslator
{
    internal static NormalizedCategory? Translate(IRevitCategoryHandle? handle)
    {
        if (handle is null)
        {
            return null;
        }

        return new NormalizedCategory
        {
            Identifier = NormalizedIdentifierFactory.Create(handle.Id, ObjectTranslationSourceKinds.Category, scope: null),
            Name = handle.Name,
            Metadata = MetadataCopier.Copy(handle.Metadata)
        };
    }
}
