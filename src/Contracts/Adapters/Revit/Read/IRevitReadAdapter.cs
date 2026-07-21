using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Composition contract for the Revit Adapter read layer.
/// </summary>
public interface IRevitReadAdapter
{
    IFamilyProvider Families { get; }

    IParameterProvider Parameters { get; }

    IRelationshipProvider Relationships { get; }

    IObjectTranslator Translator { get; }
}
