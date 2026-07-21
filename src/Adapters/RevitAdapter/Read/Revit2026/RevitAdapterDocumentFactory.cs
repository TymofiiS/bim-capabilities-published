using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Read.Revit2026;
using BIMCapabilities.Adapters.Revit.Translation.Revit2026;

namespace BIMCapabilities.Adapters.Revit.Read.Revit2026;

/// <summary>
/// Creates an operational Revit Adapter from an open Revit document.
/// </summary>
public static class RevitAdapterDocumentFactory
{
    public static RevitAdapter CreateOperational(Document document)
    {
        return CreateOperational(document, progressReporter: null);
    }

    public static RevitAdapter CreateOperational(
        Document document,
        Action<int, int, string>? progressReporter)
    {
        ArgumentGuard.ThrowIfNull(document);

        return RevitAdapter.CreateOperational(
            new Revit2026FamilyCatalog(document, progressReporter),
            new Revit2026ParameterCatalog(document),
            new Revit2026RelationshipCatalog(document),
            new Revit2026ObjectResolver(document));
    }
}
