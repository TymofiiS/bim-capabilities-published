using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Validates BIMRule capability references against a known capability registry.
/// </summary>
public interface ICapabilityCompatibilityValidator
{
    CapabilityValidationResult Validate(BimRule? rule);
}
