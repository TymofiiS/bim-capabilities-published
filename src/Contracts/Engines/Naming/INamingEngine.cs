namespace BIMCapabilities.Contracts.Engines.Naming;

/// <summary>
/// Contract for Naming Engine validation and correction operations.
/// </summary>
public interface INamingEngine
{
    NamingValidationResult Validate(NamingValidationRequest request);
}
