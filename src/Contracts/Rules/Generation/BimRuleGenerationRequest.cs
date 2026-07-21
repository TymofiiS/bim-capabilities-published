namespace BIMCapabilities.Contracts.Rules.Generation;

/// <summary>
/// Input for generating a BIMRule from natural language.
/// </summary>
public sealed record BimRuleGenerationRequest
{
    public required string NaturalLanguagePrompt { get; init; }

    public string? OutputDirectory { get; init; }

    public string? Author { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }

    public string? CorrelationId { get; init; }
}
