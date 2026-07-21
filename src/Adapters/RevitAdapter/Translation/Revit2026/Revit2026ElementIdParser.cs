using Autodesk.Revit.DB;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal static class Revit2026ElementIdParser
{
    internal static bool TryParse(string sourceObjectId, out ElementId elementId)
    {
        elementId = ElementId.InvalidElementId;

        if (string.IsNullOrWhiteSpace(sourceObjectId))
        {
            return false;
        }

        if (!long.TryParse(sourceObjectId, out var value))
        {
            return false;
        }

        elementId = new ElementId(value);
        return true;
    }
}
