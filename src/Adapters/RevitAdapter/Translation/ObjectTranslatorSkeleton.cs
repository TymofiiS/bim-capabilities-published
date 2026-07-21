using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Translation;

/// <summary>
/// Skeleton object translator that returns deterministic stub responses.
/// </summary>
public sealed class ObjectTranslatorSkeleton : IObjectTranslator
{
    public ObjectTranslationResult Translate(ObjectTranslationQuery query)
    {
        ArgumentGuard.ThrowIfNull(query);

        return RevitReadStubResponses.CreateObjectTranslationResult(query);
    }
}
