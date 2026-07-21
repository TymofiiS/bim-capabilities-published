namespace BIMCapabilities.Contracts.Engines.Family;

/// <summary>
/// Severity classification for Family Engine diagnostics.
/// </summary>
public enum FamilyEngineDiagnosticSeverity
{
    Information,
    Warning,
    Error
}

/// <summary>
/// Diagnostic emitted during Family Engine operations.
/// </summary>
public sealed record FamilyEngineDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public FamilyEngineDiagnosticSeverity Severity { get; init; }

    public string? Location { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}
