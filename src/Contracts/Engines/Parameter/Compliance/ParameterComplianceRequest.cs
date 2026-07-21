using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Parameter.Value;

namespace BIMCapabilities.Contracts.Engines.Parameter.Compliance;

/// <summary>
/// Input for the Parameter Engine compliance composition workflow.
/// </summary>
public sealed record ParameterComplianceRequest
{
    public required ParameterTargetSet TargetSet { get; init; }

    public ParameterQueryResult? ParameterQueryResult { get; init; }

    public ParameterSharedParameterFileReference? SharedParameterFile { get; init; }

    public IReadOnlyList<string>? RequiredParameterNames { get; init; }

    public IReadOnlyList<string>? SharedParameterNamesToValidate { get; init; }

    public IReadOnlyList<ParameterValueRule>? ValueRules { get; init; }

    /// <summary>
    /// Shared-parameter binding per parameter name. <c>true</c> = instance, <c>false</c> = type (default).
    /// </summary>
    public IReadOnlyDictionary<string, bool>? ParameterBindings { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
