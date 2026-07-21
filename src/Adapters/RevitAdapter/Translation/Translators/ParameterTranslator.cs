using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Mapping;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Translators;

internal static class ParameterTranslator
{
    internal static NormalizedParameter? Translate(IRevitParameterHandle? handle)
    {
        if (handle is null)
        {
            return null;
        }

        return new NormalizedParameter
        {
            Identifier = NormalizedIdentifierFactory.Create(handle.Id, ObjectTranslationSourceKinds.Parameter, scope: null),
            Name = handle.Name,
            Value = handle.Value,
            StorageType = ParameterStorageTypeMapper.Map(handle.StorageType),
            IsSharedParameter = handle.IsSharedParameter,
            Metadata = MetadataCopier.Copy(handle.Metadata)
        };
    }

    internal static IReadOnlyList<NormalizedParameter>? TranslateCollection(IReadOnlyList<IRevitParameterHandle>? handles)
    {
        if (handles is null || handles.Count == 0)
        {
            return null;
        }

        return handles
            .Select(Translate)
            .Where(parameter => parameter is not null)
            .Cast<NormalizedParameter>()
            .OrderBy(parameter => parameter.Identifier.Id, StringComparer.Ordinal)
            .ToList();
    }
}
