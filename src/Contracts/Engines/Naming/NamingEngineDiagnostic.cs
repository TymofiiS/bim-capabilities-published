namespace BIMCapabilities.Contracts.Engines.Naming;

/// <summary>
/// Severity classification for Naming Engine diagnostics.
/// </summary>
public enum NamingEngineDiagnosticSeverity
{
    Information,

    Warning,

    Error
}

/// <summary>
/// Diagnostic emitted during Naming Engine operations.
/// </summary>
public sealed record NamingEngineDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public NamingEngineDiagnosticSeverity Severity { get; init; }

    public string? Location { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}
