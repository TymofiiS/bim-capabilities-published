namespace BIMCapabilities.Contracts.Evidence;

/// <summary>
/// Identifies the platform-neutral target associated with evidence.
/// </summary>
public sealed record EvidenceTarget
{
    public required string TargetType { get; init; }

    public string? TargetId { get; init; }

    public string? TargetName { get; init; }

    public string? TargetSetDescription { get; init; }
}
