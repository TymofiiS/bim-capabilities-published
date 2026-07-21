namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Approved MVP parameters supported by the Revit Adapter read layer.
/// </summary>
internal static class SupportedMvpParameters
{
    internal const string RoomName = "RoomName";

    internal const string FireRating = "FireRating";

    internal const string AcousticRating = "AcousticRating";

    internal const string Manufacturer = "Manufacturer";

    internal static readonly ISet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        RoomName,
        FireRating,
        AcousticRating,
        Manufacturer
    };
}
