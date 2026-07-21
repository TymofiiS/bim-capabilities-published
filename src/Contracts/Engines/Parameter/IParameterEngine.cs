namespace BIMCapabilities.Contracts.Engines.Parameter;

/// <summary>
/// Contract for Parameter Engine validation and correction operations.
/// </summary>
public interface IParameterEngine
{
    ParameterValidationResult Validate(ParameterValidationRequest request);
}
