using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Composition.Fix;

/// <summary>
/// Input for building parameter fix write requests from a completed validation run.
/// </summary>
public sealed record FixPipelineRequest
{
    public required ValidationPipelineResult ValidationResult { get; init; }

    public required string RuleFilePath { get; init; }

    public string? SharedParameterFilePathOverride { get; init; }

    public ExecutionScope? Scope { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }
}
