namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Deterministic diagnostic codes emitted by the parameter retrieval provider.
/// </summary>
internal static class ParameterRetrievalDiagnostics
{
    internal const string ParameterNotFound = "ParameterQuery.ParameterNotFound";

    internal const string InvalidQuery = "ParameterQuery.InvalidQuery";

    internal const string EmptyResult = "ParameterQuery.EmptyResult";

    internal const string UnsupportedStorageType = "ParameterQuery.UnsupportedStorageType";

    internal const string UnsupportedFilter = "ParameterQuery.UnsupportedFilter";
}
