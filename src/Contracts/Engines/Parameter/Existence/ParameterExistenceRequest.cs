using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter;

namespace BIMCapabilities.Contracts.Engines.Parameter.Existence;

/// <summary>
/// Input for the Parameter Engine parameter existence validation atom.
/// </summary>
public sealed record ParameterExistenceRequest
{
    public required ParameterTargetSet TargetSet { get; init; }

    public ParameterQueryResult? ParameterQueryResult { get; init; }

    public required IReadOnlyList<string> RequiredParameterNames { get; init; }

    /// <summary>
    /// Parameter name to instance-binding flag. Instance-bound parameters are checked at family level.
    /// </summary>
    public IReadOnlyDictionary<string, bool>? ParameterBindings { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
