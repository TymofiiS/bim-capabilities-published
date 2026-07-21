using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Integration.Tests.Fixtures;

namespace BIMCapabilities.Integration.Tests;

public class DemoWorkflowValidationTests
{
    private static readonly string DemoRulesDirectory = ResolveDemoRulesDirectory();

    [Theory]
    [InlineData("DEMO-RULE-A-DOORS.bimrule")]
    [InlineData("DEMO-RULE-B-WINDOWS.bimrule")]
    [InlineData("DEMO-RULE-C-FURNITURE.bimrule")]
    public void Official_demo_rules_load_and_validate_successfully(string ruleFileName)
    {
        var rulePath = Path.Combine(DemoRulesDirectory, ruleFileName);
        Assert.True(File.Exists(rulePath), $"Missing demo rule: {rulePath}");

        var loader = new BimRuleLoader();
        var loadResult = loader.Load(rulePath);

        Assert.True(loadResult.Success, string.Join("; ", loadResult.Diagnostics.Select(d => d.Message)));
        Assert.NotNull(loadResult.Rule);

        var structureValidation = new BimRuleValidator().Validate(loadResult.Rule);
        var capabilityValidation = new CapabilityCompatibilityValidator().Validate(loadResult.Rule);

        Assert.True(structureValidation.IsValid);
        Assert.True(capabilityValidation.IsValid);
    }

    [Fact]
    public void Demo_rule_a_executes_against_mvp_provider()
    {
        var result = ExecuteDemoRule("DEMO-RULE-A-DOORS.bimrule", MvpValidationScenario.DoorPass);
        Assert.True(result.RuleValidationSucceeded);
        Assert.Equal("Completed", result.ExecutionResult!.Status.ToString());
    }

    [Fact]
    public void Demo_rule_b_executes_against_mvp_provider()
    {
        var result = ExecuteDemoRule("DEMO-RULE-B-WINDOWS.bimrule", MvpValidationScenario.WindowPass);
        Assert.True(result.RuleValidationSucceeded);
        Assert.Equal("Completed", result.ExecutionResult!.Status.ToString());
    }

    [Fact]
    public void Demo_rule_c_executes_against_mvp_provider()
    {
        var result = ExecuteDemoRule("DEMO-RULE-C-FURNITURE.bimrule", MvpValidationScenario.FurniturePass);
        Assert.True(result.RuleValidationSucceeded);
        Assert.Equal("Completed", result.ExecutionResult!.Status.ToString());
    }

    [Fact]
    public void Demo_rules_use_relative_shared_parameter_path()
    {
        var rulePath = Path.Combine(DemoRulesDirectory, "DEMO-RULE-A-DOORS.bimrule");
        var loader = new BimRuleLoader();
        var rule = loader.Load(rulePath).Rule!;

        var sharedParameterPath = rule.ExternalReferences!.Single().Location;
        Assert.Equal("../shared-parameters/CompanySharedParameters.txt", sharedParameterPath);

        var resolvedPath = Path.GetFullPath(Path.Combine(DemoRulesDirectory, sharedParameterPath));
        Assert.True(File.Exists(resolvedPath));
    }

    private static ValidationPipelineResult ExecuteDemoRule(string ruleFileName, MvpValidationScenario scenario)
    {
        var rulePath = Path.Combine(DemoRulesDirectory, ruleFileName);
        var sharedParameterPath = Path.GetFullPath(
            Path.Combine(DemoRulesDirectory, "..", "shared-parameters", "CompanySharedParameters.txt"));

        return new ValidationPipeline().Execute(new ValidationPipelineRequest
        {
            RuleFilePath = rulePath,
            FamilyProvider = new MvpFamilyProvider(scenario),
            SharedParameterFilePathOverride = sharedParameterPath,
            Scope = new ExecutionScope
            {
                ScopeType = "EntireModel",
                TargetDescription = "Demo workflow fixture model"
            },
            Environment = new ExecutionEnvironment
            {
                Platform = "Revit",
                PlatformVersion = "2026",
                ModelName = "Demo Workflow Fixture.rvt"
            }
        });
    }

    private static string ResolveDemoRulesDirectory()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "demo", "rules")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "demo", "rules")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "demo", "rules"))
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Demo rules directory was not found.");
    }
}
