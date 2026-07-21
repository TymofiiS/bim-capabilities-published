namespace BIMCapabilities.Contracts.Engines.Parameter.Write;

/// <summary>
/// Diagnostic emitted while converting parameter findings into write requests.
/// </summary>
public sealed record ParameterWriteRequestBuildDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public ParameterWriteRequestBuildDiagnosticSeverity Severity { get; init; }

    public string? ParameterName { get; init; }

    public string? ObjectId { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}
