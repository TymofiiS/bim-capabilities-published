namespace BIMCapabilities.Adapters.Revit.Translation.Mapping;

internal static class MetadataCopier
{
    internal static IReadOnlyDictionary<string, string>? Copy(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        return metadata
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }
}
