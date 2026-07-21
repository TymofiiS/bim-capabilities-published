using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter;

namespace BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

/// <summary>
/// Input for the Parameter Engine shared parameter validation atom.
/// </summary>
public sealed record SharedParameterValidationRequest
{
    public required ParameterTargetSet TargetSet { get; init; }

    public ParameterQueryResult? ParameterQueryResult { get; init; }

    public required ParameterSharedParameterFileReference SharedParameterFile { get; init; }

    public IReadOnlyList<string>? ParameterNamesToValidate { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
