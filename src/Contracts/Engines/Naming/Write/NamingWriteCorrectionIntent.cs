namespace BIMCapabilities.Contracts.Engines.Naming.Write;

/// <summary>
/// Deterministic rename intent applied when generating naming write requests.
/// </summary>
public sealed record NamingWriteCorrectionIntent
{
    public required string ObjectId { get; init; }

    public string? ProposedName { get; init; }

    public string? RequestedAction { get; init; }
}
