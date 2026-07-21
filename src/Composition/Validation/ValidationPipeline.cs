using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Versions;
using BIMCapabilities.Runtime;

namespace BIMCapabilities.Composition.Validation;

/// <summary>
/// Operational validation pipeline composing rule loading, engine execution, evidence collection, and reporting.
/// </summary>
public sealed class ValidationPipeline : IValidationPipeline
{
    internal const string PipelineId = "validation-pipeline-mvp-001";

    private readonly BimRuleLoader _loader = new();
    private readonly BimRuleValidator _structureValidator = new();
    private readonly BimRuleVersionValidator _versionValidator = new();
    private readonly CapabilityCompatibilityValidator _capabilityValidator = new();
    private readonly BimRuleConfigurationValidator _configurationValidator = new();

    public ValidationPipelineResult Execute(ValidationPipelineRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.FamilyProvider);

        var correlationId = request.CorrelationId ?? $"corr-validation-{Guid.NewGuid():N}";
        var executedAt = request.ExecutedAt ?? DateTimeOffset.UtcNow;

        var loadResult = _loader.Load(request.RuleFilePath);
        if (!loadResult.Success || loadResult.Rule is null)
        {
            return ValidationPipelineSupport.CreateFailedLoadResult(loadResult);
        }

        var rule = loadResult.Rule;
        var structureValidation = _structureValidator.Validate(rule);
        var versionValidation = _versionValidator.Validate(rule);
        var capabilityValidation = MergeCapabilityValidationResults(
            _capabilityValidator.Validate(rule),
            _configurationValidator.Validate(rule));

        if (!structureValidation.IsValid || !versionValidation.IsValid || !capabilityValidation.IsValid)
        {
            return ValidationPipelineSupport.CreateFailedValidationResult(
                loadResult,
                structureValidation,
                versionValidation,
                capabilityValidation);
        }

        var runtime = new RuntimeSkeleton();
        return ValidationPipelineSupport.ExecuteWorkflow(request, rule, runtime, executedAt, correlationId);
    }

    private static CapabilityValidationResult MergeCapabilityValidationResults(
        CapabilityValidationResult first,
        CapabilityValidationResult second)
    {
        return new CapabilityValidationResult
        {
            Diagnostics = first.Diagnostics.Concat(second.Diagnostics).ToArray()
        };
    }
}
