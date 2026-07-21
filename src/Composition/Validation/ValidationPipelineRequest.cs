using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Execution.Logging;

namespace BIMCapabilities.Composition.Validation;

/// <summary>
/// Input for executing the end-to-end validation pipeline.
/// </summary>
public sealed record ValidationPipelineRequest
{
    public required string RuleFilePath { get; init; }

    public required IFamilyProvider FamilyProvider { get; init; }

    public required ExecutionScope Scope { get; init; }

    public required ExecutionEnvironment Environment { get; init; }

    public string? SharedParameterFilePathOverride { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public IExecutionLog? ExecutionLog { get; init; }

    public Action<int, int, string>? ProgressReporter { get; init; }
}
