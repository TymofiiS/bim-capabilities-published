using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Integration.Tests.Fixtures;

internal static class CapabilityCompositionFixtureBuilder
{
    internal static ValidationPipelineRequest CreateRequest(string ruleFileName, MvpValidationScenario scenario)
    {
        return new ValidationPipelineRequest
        {
            RuleFilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", ruleFileName),
            FamilyProvider = new MvpFamilyProvider(scenario),
            SharedParameterFilePathOverride = MvpValidationFixtureBuilder.GetSharedParameterFilePath(),
            Scope = new ExecutionScope
            {
                ScopeType = "EntireModel",
                TargetDescription = "Capability composition fixture model"
            },
            Environment = new ExecutionEnvironment
            {
                Platform = "Revit",
                PlatformVersion = "2026",
                ModelName = "Capability Composition Fixture.rvt"
            },
            CorrelationId = $"corr-capability-{ruleFileName}",
            ExecutedAt = MvpFamilyProvider.FixedExecutedAt
        };
    }
}
