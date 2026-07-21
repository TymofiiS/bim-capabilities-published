namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Diagnostic emitted during parameter retrieval.
/// </summary>
public sealed record ParameterQueryDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public ParameterQueryDiagnosticSeverity Severity { get; init; }

    public string? Location { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}
