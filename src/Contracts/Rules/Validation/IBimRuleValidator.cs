using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Rules.Validation;

/// <summary>
/// Validates BIMRule contract structure.
/// </summary>
public interface IBimRuleValidator
{
    BimRuleValidationResult Validate(BimRule? rule);
}
