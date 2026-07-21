namespace BIMCapabilities.Adapters.Revit.Translation.Abstractions;

/// <summary>
/// Resolves Revit source object identifiers into testable translation handles.
/// </summary>
public interface IRevitObjectResolver
{
    IRevitFamilyHandle? ResolveFamily(string sourceObjectId);

    IRevitFamilyTypeHandle? ResolveFamilyType(string sourceObjectId);

    IRevitCategoryHandle? ResolveCategory(string sourceObjectId);

    IRevitParameterHandle? ResolveParameter(string sourceObjectId);

    IRevitElementHandle? ResolveElement(string sourceObjectId);

    IRevitRelationshipHandle? ResolveRelationship(string sourceObjectId);
}
