namespace BIMCapabilities.Contracts.Engines.Parameter;

/// <summary>
/// Severity classification for Parameter Engine diagnostics.
/// </summary>
public enum ParameterEngineDiagnosticSeverity
{
    Information,

    Warning,

    Error
}

/// <summary>
/// Diagnostic emitted during Parameter Engine operations.
/// </summary>
public sealed record ParameterEngineDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public ParameterEngineDiagnosticSeverity Severity { get; init; }

    public string? Location { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}
