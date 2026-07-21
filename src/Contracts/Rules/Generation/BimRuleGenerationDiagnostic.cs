namespace BIMCapabilities.Contracts.Rules.Generation;

/// <summary>
/// Severity classification for BIMRule generation diagnostics.
/// </summary>
public enum BimRuleGenerationDiagnosticSeverity
{
    Information,
    Warning,
    Error
}

/// <summary>
/// Diagnostic emitted during BIMRule generation.
/// </summary>
public sealed record BimRuleGenerationDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public BimRuleGenerationDiagnosticSeverity Severity { get; init; }

    public string? Source { get; init; }
}
