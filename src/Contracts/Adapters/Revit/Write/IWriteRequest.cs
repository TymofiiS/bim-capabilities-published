using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Canonical write request contract produced by engines for adapter execution.
/// </summary>
public interface IWriteRequest
{
    string RequestId { get; }

    NormalizedIdentifier TargetObject { get; }

    WriteRequestType RequestType { get; }

    int Order { get; }

    IReadOnlyDictionary<string, string>? Payload { get; }

    IReadOnlyDictionary<string, string>? Metadata { get; }

    string? CorrelationId { get; }

    string? RuleId { get; }

    DateTimeOffset RequestedAt { get; }
}
