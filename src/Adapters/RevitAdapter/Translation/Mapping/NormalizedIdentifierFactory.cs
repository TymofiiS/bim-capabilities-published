using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation.Mapping;

internal static class NormalizedIdentifierFactory
{
    internal const string DefaultScope = "project-document";

    internal static NormalizedIdentifier Create(string id, string kind, string? scope = DefaultScope)
    {
        return new NormalizedIdentifier
        {
            Id = id,
            Kind = kind,
            Scope = scope
        };
    }
}
