using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Deterministic update request from an engine to the adapter.
/// </summary>
public sealed record WriteRequest : IWriteRequest
{
    public required string RequestId { get; init; }

    public required NormalizedIdentifier TargetObject { get; init; }

    public required WriteRequestType RequestType { get; init; }

    public int Order { get; init; }

    public IReadOnlyDictionary<string, string>? Payload { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string? CorrelationId { get; init; }

    public string? RuleId { get; init; }

    public DateTimeOffset RequestedAt { get; init; }
}
