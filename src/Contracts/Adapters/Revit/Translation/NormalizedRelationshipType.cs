namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Platform-neutral relationship classification between normalized objects.
/// </summary>
public enum NormalizedRelationshipType
{
    Parent,
    Child,
    Nested,
    Host,
    Reference,
    TypeDefinition,
    Other
}
