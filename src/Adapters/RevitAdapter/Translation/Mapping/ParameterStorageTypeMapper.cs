using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Mapping;

internal static class ParameterStorageTypeMapper
{
    internal static NormalizedParameterStorageType Map(string storageType)
    {
        return storageType switch
        {
            "Integer" => NormalizedParameterStorageType.Integer,
            "Double" => NormalizedParameterStorageType.Double,
            "String" => NormalizedParameterStorageType.String,
            "ElementId" => NormalizedParameterStorageType.ElementId,
            "None" => NormalizedParameterStorageType.None,
            _ when string.Equals(storageType, "Boolean", StringComparison.OrdinalIgnoreCase) =>
                NormalizedParameterStorageType.Boolean,
            _ => NormalizedParameterStorageType.None
        };
    }
}
