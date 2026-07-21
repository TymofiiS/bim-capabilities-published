namespace BIMCapabilities.Contracts.Engines.Naming.Write;

/// <summary>
/// Aggregate statistics for naming write request generation.
/// </summary>
public sealed record NamingWriteRequestBuildStatistics
{
    public int FindingsProcessed { get; init; }

    public int RequestsGenerated { get; init; }

    public int RenameFamilyRequests { get; init; }

    public int RenameTypeRequests { get; init; }

    public int SkippedFindings { get; init; }
}
