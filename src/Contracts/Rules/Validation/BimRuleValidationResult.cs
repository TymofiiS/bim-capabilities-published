namespace BIMCapabilities.Contracts.Rules.Validation;

/// <summary>
/// Result of structural BIMRule validation.
/// </summary>
public sealed record BimRuleValidationResult
{
    public IReadOnlyList<BimRuleValidationDiagnostic> Diagnostics { get; init; } = [];

    public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != ValidationSeverity.Error);
}
