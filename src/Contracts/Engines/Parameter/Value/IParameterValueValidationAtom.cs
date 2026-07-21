namespace BIMCapabilities.Contracts.Engines.Parameter.Value;

/// <summary>
/// Contract for the Parameter Engine parameter value validation atom.
/// </summary>
public interface IParameterValueValidationAtom
{
    string AtomId { get; }

    ParameterValueValidationResult Validate(ParameterValueValidationRequest request);
}
