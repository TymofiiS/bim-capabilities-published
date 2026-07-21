namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Deterministic diagnostic codes emitted by the family retrieval provider.
/// </summary>
internal static class FamilyRetrievalDiagnostics
{
    internal const string NoFamiliesFound = "FamilyQuery.NoFamiliesFound";

    internal const string InvalidCategory = "FamilyQuery.InvalidCategory";

    internal const string InvalidQuery = "FamilyQuery.InvalidQuery";

    internal const string EmptyResult = "FamilyQuery.EmptyResult";

    internal const string UnsupportedFilter = "FamilyQuery.UnsupportedFilter";
}
