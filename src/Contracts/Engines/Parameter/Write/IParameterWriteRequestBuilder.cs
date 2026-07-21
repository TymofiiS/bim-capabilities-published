namespace BIMCapabilities.Contracts.Engines.Parameter.Write;

/// <summary>
/// Converts parameter compliance findings into deterministic write requests.
/// </summary>
public interface IParameterWriteRequestBuilder
{
    string BuilderId { get; }

    ParameterWriteRequestBuildResult Build(ParameterWriteRequestBuildRequest request);
}
