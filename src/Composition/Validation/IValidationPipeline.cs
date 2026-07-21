namespace BIMCapabilities.Composition.Validation;

/// <summary>
/// Executes the complete BIMCapabilities validation workflow from rule to report.
/// </summary>
public interface IValidationPipeline
{
    ValidationPipelineResult Execute(ValidationPipelineRequest request);
}
