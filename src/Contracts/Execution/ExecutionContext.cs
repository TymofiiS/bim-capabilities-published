using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Execution;

/// <summary>
/// Shared runtime input that carries everything required to execute a BIMRule workflow.
/// </summary>
public sealed record ExecutionContext
{
    public required BimRule Rule { get; init; }

    public string? RuleSourcePath { get; init; }

    public required ExecutionRequest Request { get; init; }

    public required ExecutionScope Scope { get; init; }

    public required ExecutionEnvironment Environment { get; init; }

    public required string CorrelationId { get; init; }

    public string? ParentCorrelationId { get; init; }

    public string? TraceId { get; init; }
}
