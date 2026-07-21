namespace BIMCapabilities.Contracts.Rules.Generation;

/// <summary>
/// Human-readable summary of what the generator detected and produced.
/// </summary>
public sealed record BimRuleGenerationReport
{
    public required string GeneratedRuleName { get; init; }

    public IReadOnlyList<string> DetectedCategories { get; init; } = [];

    public IReadOnlyList<string> DetectedParameters { get; init; } = [];

    public IReadOnlyList<string> DetectedNamingRules { get; init; } = [];

    public IReadOnlyList<string> DetectedComplianceRules { get; init; } = [];

    public string? SharedParameterFilePath { get; init; }
}
