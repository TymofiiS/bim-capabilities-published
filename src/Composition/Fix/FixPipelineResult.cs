using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Composition.Fix;

/// <summary>
/// Outcome of fix write-request generation from validation findings.
/// </summary>
public sealed record FixPipelineResult
{
    public required bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public IReadOnlyList<WriteRequest> WriteRequests { get; init; } = [];

    public ParameterWriteRequestBuildStatistics? Statistics { get; init; }

    public FixCorrectionSummary? CorrectionSummary { get; init; }

    public string? SharedParameterFilePath { get; init; }
}

/// <summary>
/// Business summary of planned or executed parameter corrections.
/// </summary>
public sealed record FixCorrectionSummary
{
    public int ParametersAdded { get; init; }

    public int ValuesAssigned { get; init; }

    public int AffectedTypes { get; init; }

    public int NamesRenamed { get; init; }

    public IReadOnlyList<string> DefaultValuesApplied { get; init; } = [];
}
