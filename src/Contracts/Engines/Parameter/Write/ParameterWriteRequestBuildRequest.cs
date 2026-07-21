using BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

namespace BIMCapabilities.Contracts.Engines.Parameter.Write;

/// <summary>
/// Input for converting parameter compliance findings into write requests.
/// </summary>
public sealed record ParameterWriteRequestBuildRequest
{
    public required ParameterComplianceResult ComplianceResult { get; init; }

    public required ParameterTargetSet TargetSet { get; init; }

    public IReadOnlyList<SharedParameterDefinition>? SharedParameterDefinitions { get; init; }

    public IReadOnlyList<ParameterWriteCorrectionIntent>? CorrectionIntents { get; init; }

    public IReadOnlyDictionary<string, string>? ParameterDefaults { get; init; }

    /// <summary>
    /// Shared-parameter binding per parameter name. <c>true</c> = instance, <c>false</c> = type (default).
    /// </summary>
    public IReadOnlyDictionary<string, bool>? ParameterBindings { get; init; }

    public IReadOnlyDictionary<string, string>? ParameterFillRules { get; init; }

    public DateTimeOffset RequestedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
