using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Read;

internal sealed class RevitParameterCatalogEntry : IRevitParameterCatalogEntry
{
    public required IRevitParameterHandle Parameter { get; init; }

    public string? CategoryName { get; init; }

    public string? FamilyId { get; init; }

    public string? FamilyName { get; init; }

    public string? FamilyTypeId { get; init; }

    public string? FamilyTypeName { get; init; }
}
