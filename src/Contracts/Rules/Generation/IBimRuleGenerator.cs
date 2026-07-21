namespace BIMCapabilities.Contracts.Rules.Generation;

/// <summary>
/// Generates executable BIMRule documents from natural language descriptions.
/// </summary>
public interface IBimRuleGenerator
{
    BimRuleGenerationResult Generate(BimRuleGenerationRequest request);
}
