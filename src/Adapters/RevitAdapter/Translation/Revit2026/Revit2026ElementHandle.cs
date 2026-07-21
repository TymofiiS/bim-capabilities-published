using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal sealed class Revit2026ElementHandle : IRevitElementHandle
{
    public Revit2026ElementHandle(Element element)
    {
        ArgumentGuard.ThrowIfNull(element);

        Id = element.Id.ToString();
        Name = element.Name ?? string.Empty;
        Category = element.Category is null ? null : new Revit2026CategoryHandle(element.Category);
        Parameters = Revit2026ParameterCollector.Collect(element);
        Relationships = [];
        Metadata = null;
    }

    public string Id { get; }

    public string Name { get; }

    public IRevitCategoryHandle? Category { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public IReadOnlyList<IRevitParameterHandle> Parameters { get; }

    public IReadOnlyList<IRevitRelationshipHandle> Relationships { get; }
}
