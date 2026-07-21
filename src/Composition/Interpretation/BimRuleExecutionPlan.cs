namespace BIMCapabilities.Composition.Interpretation;

internal sealed record BimRuleExecutionPlan
{
    public required string RuleId { get; init; }

    public required IReadOnlyList<CategoryExecutionSpecification> Categories { get; init; }

    public bool RunParameterCompliance { get; init; }

    public bool RunNamingCompliance { get; init; }

    public bool RunImportedCadExclusion { get; init; }

    public bool GenerateReport { get; init; }

    public IReadOnlyList<string> InterpretationMarkers { get; init; } = [];
}
