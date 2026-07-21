using BIMCapabilities.Adapters.Revit.Translation.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Read.Abstractions;

/// <summary>
/// A parameter exposed by the Revit model together with retrieval context.
/// </summary>
public interface IRevitParameterCatalogEntry
{
    IRevitParameterHandle Parameter { get; }

    string? CategoryName { get; }

    string? FamilyId { get; }

    string? FamilyName { get; }

    string? FamilyTypeId { get; }

    string? FamilyTypeName { get; }
}
