namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Approved MVP family categories supported by the Revit Adapter read layer.
/// </summary>
internal static class SupportedFamilyCategories
{
    internal const string Doors = "Doors";

    internal const string Windows = "Windows";

    internal const string GenericModels = "Generic Models";

    internal static readonly ISet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        Doors,
        Windows,
        GenericModels
    };
}
