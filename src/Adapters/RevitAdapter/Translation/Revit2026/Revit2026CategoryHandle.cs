using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

internal sealed class Revit2026CategoryHandle : IRevitCategoryHandle
{
    public Revit2026CategoryHandle(Category category)
    {
        ArgumentGuard.ThrowIfNull(category);

        Id = category.Id.ToString();
        Name = category.Name;
        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["builtInCategory"] = category.BuiltInCategory.ToString()
        };
    }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }
}
