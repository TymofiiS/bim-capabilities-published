namespace BIMCapabilities.Adapters.Revit.Translation;

/// <summary>
/// Supported <see cref="BIMCapabilities.Contracts.Adapters.Revit.Translation.ObjectTranslationQuery.SourceKind"/> values.
/// </summary>
internal static class ObjectTranslationSourceKinds
{
    internal const string Family = "family";

    internal const string FamilyType = "familyType";

    internal const string Category = "category";

    internal const string Parameter = "parameter";

    internal const string Element = "element";

    internal const string Relationship = "relationship";
}
