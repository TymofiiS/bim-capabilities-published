using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Rules.Validation.Versions;

/// <summary>
/// Validates BIMRule contract version compatibility.
/// </summary>
public interface IBimRuleVersionValidator
{
    VersionValidationResult Validate(BimRule? rule);
}
