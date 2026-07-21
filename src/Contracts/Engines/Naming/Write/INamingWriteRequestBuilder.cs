namespace BIMCapabilities.Contracts.Engines.Naming.Write;

/// <summary>
/// Converts naming compliance findings into deterministic write requests.
/// </summary>
public interface INamingWriteRequestBuilder
{
    string BuilderId { get; }

    NamingWriteRequestBuildResult Build(NamingWriteRequestBuildRequest request);
}
