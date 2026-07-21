using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter;

namespace BIMCapabilities.Contracts.Engines.Parameter.Value;

/// <summary>
/// Input for the Parameter Engine parameter value validation atom.
/// </summary>
public sealed record ParameterValueValidationRequest
{
    public required ParameterTargetSet TargetSet { get; init; }

    public ParameterQueryResult? ParameterQueryResult { get; init; }

    public required IReadOnlyList<ParameterValueRule> Rules { get; init; }

    /// <summary>
    /// Shared-parameter binding per parameter name. <c>true</c> = instance, <c>false</c> = type (default).
    /// </summary>
    public IReadOnlyDictionary<string, bool>? ParameterBindings { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
