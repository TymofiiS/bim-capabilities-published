namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Reference to an individual write request within a batch result.
/// </summary>
public sealed record WriteRequestReference
{
    public required string RequestId { get; init; }

    public WriteRequestType RequestType { get; init; }

    public WriteRequestStatus Status { get; init; }

    public int Order { get; init; }
}
