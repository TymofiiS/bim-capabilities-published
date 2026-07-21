using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

namespace BIMCapabilities.Contracts.Engines.Parameter.Write;

/// <summary>
/// Deterministic correction intent applied when generating parameter write requests.
/// </summary>
public sealed record ParameterWriteCorrectionIntent
{
    public required string ParameterName { get; init; }

    public string? ObjectId { get; init; }

    public WriteRequestType? RequestedAction { get; init; }

    public string? RequestedValue { get; init; }
}
