namespace BIMCapabilities.Contracts.Engines.Parameter.Compliance;

/// <summary>
/// Contract for the Parameter Engine compliance composition layer.
/// </summary>
public interface IParameterComplianceEngine
{
    string EngineId { get; }

    ParameterComplianceResult Evaluate(ParameterComplianceRequest request);
}
