using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Translation.Mapping;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Translators;

internal static class PlacedInstanceTranslator
{
    internal static NormalizedIdentifier CreateIdentity(FamilyInstance instance)
    {
        return NormalizedIdentifierFactory.Create(
            instance.Id.ToString(),
            ObjectTranslationSourceKinds.Element);
    }
}
