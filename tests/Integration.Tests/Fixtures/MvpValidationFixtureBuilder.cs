using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;

namespace BIMCapabilities.Integration.Tests.Fixtures;

internal static class MvpValidationFixtureBuilder
{
    internal const string CorrelationId = "corr-mvp-validation-001";

    internal static ValidationPipelineRequest CreateRequest(MvpValidationScenario scenario)
    {
        return new ValidationPipelineRequest
        {
            RuleFilePath = GetRulePath(),
            FamilyProvider = new MvpFamilyProvider(scenario),
            SharedParameterFilePathOverride = GetSharedParameterFilePath(),
            Scope = new ExecutionScope
            {
                ScopeType = "EntireModel",
                TargetDescription = "MVP validation fixture model"
            },
            Environment = new ExecutionEnvironment
            {
                Platform = "Revit",
                PlatformVersion = "2026",
                ModelName = "MVP Validation Fixture.rvt"
            },
            CorrelationId = CorrelationId,
            ExecutedAt = MvpFamilyProvider.FixedExecutedAt
        };
    }

    internal static string GetRulePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "STD-ARC-OPENINGS-V01.bimrule");
    }

    internal static string GetSharedParameterFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "CompanySharedParameters.txt");
    }
}
