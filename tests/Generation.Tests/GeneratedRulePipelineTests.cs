using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Rules.Generation;
using BIMCapabilities.Generation.Tests.Fixtures;

namespace BIMCapabilities.Generation.Tests;

public class GeneratedRulePipelineTests
{
    private readonly IBimRuleGenerator _generator = new BimRuleGenerator();
    private readonly ValidationPipeline _pipeline = new();

    [Fact]
    public void Generated_arc_openings_rule_executes_through_mvp_pipeline()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "bim-generation-pipeline", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var generationResult = _generator.Generate(new BimRuleGenerationRequest
            {
                NaturalLanguagePrompt = BimRuleGenerationTestPrompts.ArchitectureOpeningsPrompt,
                OutputDirectory = outputDirectory,
                Author = "MVP-003 Demo"
            });

            Assert.True(generationResult.Success);
            Assert.NotNull(generationResult.OutputFilePath);

            var pipelineResult = _pipeline.Execute(new ValidationPipelineRequest
            {
                RuleFilePath = generationResult.OutputFilePath,
                FamilyProvider = new MvpFamilyProvider(MvpValidationScenario.DemoPass),
                SharedParameterFilePathOverride = GetSharedParameterFilePath(),
                Scope = new ExecutionScope
                {
                    ScopeType = "EntireModel",
                    TargetDescription = "MVP-003 generated rule validation"
                },
                Environment = new ExecutionEnvironment
                {
                    Platform = "Revit",
                    PlatformVersion = "2026",
                    ModelName = "MVP-003 Generated Rule Fixture.rvt"
                },
                CorrelationId = "corr-mvp-003-generated-rule",
                ExecutedAt = MvpFamilyProvider.FixedExecutedAt
            });

            Assert.True(pipelineResult.LoadResult.Success);
            Assert.Equal("STD-ARC-OPENINGS-V01", pipelineResult.LoadResult.Rule!.Metadata.RuleId);
            Assert.True(pipelineResult.RuleValidationSucceeded);
            Assert.Equal(ExecutionStatus.Completed, pipelineResult.ExecutionResult!.Status);
            Assert.NotNull(pipelineResult.ReportOutput);
            Assert.NotNull(pipelineResult.HtmlReport);
            Assert.NotNull(pipelineResult.JsonReport);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static string GetSharedParameterFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "CompanySharedParameters.txt");
    }
}
