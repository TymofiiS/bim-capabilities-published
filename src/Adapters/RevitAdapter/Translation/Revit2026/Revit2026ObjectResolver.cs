using Autodesk.Revit.DB;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;

namespace BIMCapabilities.Adapters.Revit.Translation.Revit2026;

/// <summary>
/// Resolves Revit 2026 document objects into translation handles.
/// </summary>
public sealed class Revit2026ObjectResolver : IRevitObjectResolver
{
    private readonly Document _document;

    public Revit2026ObjectResolver(Document document)
    {
        ArgumentGuard.ThrowIfNull(document);
        _document = document;
    }

    public IRevitFamilyHandle? ResolveFamily(string sourceObjectId)
    {
        if (!Revit2026ElementIdParser.TryParse(sourceObjectId, out var elementId))
        {
            return null;
        }

        return _document.GetElement(elementId) is Family family
            ? new Revit2026FamilyHandle(family, _document)
            : null;
    }

    public IRevitFamilyTypeHandle? ResolveFamilyType(string sourceObjectId)
    {
        if (!Revit2026ElementIdParser.TryParse(sourceObjectId, out var elementId))
        {
            return null;
        }

        return _document.GetElement(elementId) is FamilySymbol symbol
            ? new Revit2026FamilyTypeHandle(
                symbol,
                _document,
                Revit2026FamilyParameterContextCollector.Collect(symbol.Family, _document))
            : null;
    }

    public IRevitCategoryHandle? ResolveCategory(string sourceObjectId)
    {
        if (!Revit2026ElementIdParser.TryParse(sourceObjectId, out var elementId))
        {
            return null;
        }

        var category = Category.GetCategory(_document, elementId);
        return category is null ? null : new Revit2026CategoryHandle(category);
    }

    public IRevitParameterHandle? ResolveParameter(string sourceObjectId)
    {
        var separatorIndex = sourceObjectId.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= sourceObjectId.Length - 1)
        {
            return null;
        }

        var ownerIdText = sourceObjectId[..separatorIndex];
        var parameterIdText = sourceObjectId[(separatorIndex + 1)..];

        if (!Revit2026ElementIdParser.TryParse(ownerIdText, out var ownerElementId))
        {
            return null;
        }

        if (_document.GetElement(ownerElementId) is not Element ownerElement)
        {
            return null;
        }

        var parameter = ownerElement
            .GetOrderedParameters()
            .FirstOrDefault(candidate => string.Equals(candidate.Id.ToString(), parameterIdText, StringComparison.Ordinal));

        return parameter is null ? null : new Revit2026ParameterHandle(parameter);
    }

    public IRevitElementHandle? ResolveElement(string sourceObjectId)
    {
        if (!Revit2026ElementIdParser.TryParse(sourceObjectId, out var elementId))
        {
            return null;
        }

        return _document.GetElement(elementId) is Element element
            ? new Revit2026ElementHandle(element)
            : null;
    }

    public IRevitRelationshipHandle? ResolveRelationship(string sourceObjectId)
    {
        return null;
    }
}
