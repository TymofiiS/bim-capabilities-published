namespace BIMCapabilities.Contracts.Engines.Naming.Compliance;

/// <summary>
/// Contract for the Naming Engine compliance composition layer.
/// </summary>
public interface INamingComplianceEngine
{
    string EngineId { get; }

    NamingComplianceResult Evaluate(NamingComplianceRequest request);
}
