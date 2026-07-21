namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Relationship classification supported by relationship retrieval queries.
/// </summary>
public enum RelationshipType
{
    ParentChild,
    NestedFamily,
    ImportedCad,
    FamilyType,
    Host,
    Dependency,
    Reference,
    Custom
}
