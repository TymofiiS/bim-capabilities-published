using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Contracts.Engines.Parameter.Write;

/// <summary>
/// Result of converting parameter compliance findings into write requests.
/// </summary>
public sealed record ParameterWriteRequestBuildResult
{
    public required string BuilderId { get; init; }

    public IReadOnlyList<WriteRequest>? WriteRequests { get; init; }

    public IReadOnlyList<ParameterWriteRequestBuildDiagnostic>? Diagnostics { get; init; }

    public ParameterWriteRequestBuildStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
