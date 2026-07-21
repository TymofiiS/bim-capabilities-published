using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Composes the Revit Adapter read layer without performing Revit API access.
/// </summary>
public sealed class RevitReadAdapter : IRevitReadAdapter
{
    public RevitReadAdapter()
        : this(null, null)
    {
    }

    public RevitReadAdapter(IObjectTranslator? translator)
        : this(translator, null)
    {
    }

    public RevitReadAdapter(IObjectTranslator? translator, IFamilyProvider? familyProvider)
        : this(translator, familyProvider, null)
    {
    }

    public RevitReadAdapter(
        IObjectTranslator? translator,
        IFamilyProvider? familyProvider,
        IParameterProvider? parameterProvider)
        : this(translator, familyProvider, parameterProvider, null)
    {
    }

    public RevitReadAdapter(
        IObjectTranslator? translator,
        IFamilyProvider? familyProvider,
        IParameterProvider? parameterProvider,
        IRelationshipProvider? relationshipProvider)
    {
        Families = familyProvider ?? new FamilyProviderSkeleton();
        Parameters = parameterProvider ?? new ParameterProviderSkeleton();
        Relationships = relationshipProvider ?? new RelationshipProviderSkeleton();
        Translator = translator ?? new ObjectTranslatorSkeleton();
    }

    public IFamilyProvider Families { get; }

    public IParameterProvider Parameters { get; }

    public IRelationshipProvider Relationships { get; }

    public IObjectTranslator Translator { get; }
}
