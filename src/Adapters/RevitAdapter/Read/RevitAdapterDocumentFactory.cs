using Autodesk.Revit.DB;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Version-neutral entry point for creating an operational Revit adapter from an open document.
/// Implementation is shared across supported Revit API years unless isolated with compile symbols.
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
        return Revit2026.RevitAdapterDocumentFactory.CreateOperational(document, progressReporter);
    }
}
