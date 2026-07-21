namespace BIMCapabilities.Contracts.Engines.Parameter.Write;

/// <summary>
/// Aggregate statistics for parameter write request generation.
/// </summary>
public sealed record ParameterWriteRequestBuildStatistics
{
    public int FindingsProcessed { get; init; }

    public int RequestsGenerated { get; init; }

    public int CreateRequests { get; init; }

    public int UpdateRequests { get; init; }

    public int DeleteRequests { get; init; }

    public int SkippedFindings { get; init; }
}
