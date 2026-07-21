using BIMCapabilities.Adapters.Revit.Read.Abstractions;
using BIMCapabilities.Adapters.Revit.Translation;
using BIMCapabilities.Adapters.Revit.Translation.Abstractions;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Read;

/// <summary>
/// Operational Revit Adapter that composes family, parameter, relationship, and translation services.
/// </summary>
public sealed class RevitAdapter : IRevitAdapter
{
    private readonly IFamilyQueryClock _clock;

    public RevitAdapter(
        IFamilyProvider familyProvider,
        IParameterProvider parameterProvider,
        IRelationshipProvider relationshipProvider,
        IObjectTranslator translator)
        : this(familyProvider, parameterProvider, relationshipProvider, translator, new SystemFamilyQueryClock())
    {
    }

    internal RevitAdapter(
        IFamilyProvider familyProvider,
        IParameterProvider parameterProvider,
        IRelationshipProvider relationshipProvider,
        IObjectTranslator translator,
        IFamilyQueryClock clock)
    {
        ArgumentGuard.ThrowIfNull(familyProvider);
        ArgumentGuard.ThrowIfNull(parameterProvider);
        ArgumentGuard.ThrowIfNull(relationshipProvider);
        ArgumentGuard.ThrowIfNull(translator);
        ArgumentGuard.ThrowIfNull(clock);

        Families = familyProvider;
        Parameters = parameterProvider;
        Relationships = relationshipProvider;
        Translator = translator;
        _clock = clock;
    }

    public IFamilyProvider Families { get; }

    public IParameterProvider Parameters { get; }

    public IRelationshipProvider Relationships { get; }

    public IObjectTranslator Translator { get; }

    public RevitAdapterReadResult Read(RevitAdapterReadContext context)
    {
        return RevitAdapterReadSupport.ExecuteRead(this, context, _clock.UtcNow);
    }

    public static RevitAdapter CreateOperational(
        IRevitFamilyCatalog familyCatalog,
        IRevitParameterCatalog parameterCatalog,
        IRevitRelationshipCatalog relationshipCatalog,
        IRevitObjectResolver objectResolver)
    {
        ArgumentGuard.ThrowIfNull(familyCatalog);
        ArgumentGuard.ThrowIfNull(parameterCatalog);
        ArgumentGuard.ThrowIfNull(relationshipCatalog);
        ArgumentGuard.ThrowIfNull(objectResolver);

        return RevitAdapterReadSupport.CreateOperationalReadLayer(
            familyCatalog,
            parameterCatalog,
            relationshipCatalog,
            objectResolver,
            new SystemFamilyQueryClock());
    }
}
