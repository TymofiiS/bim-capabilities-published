namespace BIMCapabilities.Contracts.Rules.Validation;

/// <summary>
/// Describes a structural validation outcome for a BIMRule document.
/// </summary>
public sealed record BimRuleValidationDiagnostic
{
    public required string Code { get; init; }

    public required ValidationSeverity Severity { get; init; }

    public required string Message { get; init; }

    public string? Location { get; init; }
}
