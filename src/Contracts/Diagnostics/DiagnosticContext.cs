using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Contracts.Diagnostics;

/// <summary>
/// Execution context metadata associated with a diagnostic record.
/// </summary>
public sealed record DiagnosticContext
{
    public string? RuleId { get; init; }

    public string? RuleSourcePath { get; init; }

    public ExecutionMode? ExecutionMode { get; init; }

    public string? EngineId { get; init; }

    public string? CapabilityId { get; init; }

    public string? CorrelationId { get; init; }

    public string? ParentCorrelationId { get; init; }

    public string? TraceId { get; init; }
}
