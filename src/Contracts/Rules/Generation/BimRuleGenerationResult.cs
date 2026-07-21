namespace BIMCapabilities.Contracts.Rules.Generation;

/// <summary>
/// Outcome of a BIMRule generation operation.
/// </summary>
public sealed record BimRuleGenerationResult
{
    public bool Success { get; init; }

    public BimRule? Rule { get; init; }

    public string? OutputFileName { get; init; }

    public string? OutputFilePath { get; init; }

    public string? SerializedRule { get; init; }

    public BimRuleGenerationReport? Report { get; init; }

    public bool ValidationSucceeded { get; init; }

    public IReadOnlyList<BimRuleGenerationDiagnostic>? Diagnostics { get; init; }
}
