namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Contract for translating source objects into normalized adapter contracts.
/// </summary>
public interface IObjectTranslator
{
    ObjectTranslationResult Translate(ObjectTranslationQuery query);
}
