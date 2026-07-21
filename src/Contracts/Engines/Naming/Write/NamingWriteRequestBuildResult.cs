using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Contracts.Engines.Naming.Write;

/// <summary>
/// Result of converting naming compliance findings into write requests.
/// </summary>
public sealed record NamingWriteRequestBuildResult
{
    public required string BuilderId { get; init; }

    public IReadOnlyList<WriteRequest>? WriteRequests { get; init; }

    public IReadOnlyList<NamingWriteRequestBuildDiagnostic>? Diagnostics { get; init; }

    public NamingWriteRequestBuildStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
