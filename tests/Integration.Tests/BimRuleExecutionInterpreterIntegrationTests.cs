using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Integration.Tests.Fixtures;

namespace BIMCapabilities.Integration.Tests;

public class BimRuleExecutionInterpreterIntegrationTests
{
    private readonly ValidationPipeline _pipeline = new();

    [Fact]
    public void Rule_A_validates_doors_with_fire_rating_only()
    {
        var result = _pipeline.Execute(CapabilityCompositionFixtureBuilder.CreateRequest(
            "RULE-A-DOORS-FIRE-RATING.bimrule",
            MvpValidationScenario.DemoPass));

        Assert.True(result.RuleValidationSucceeded);
        Assert.Equal(ExecutionStatus.Completed, result.ExecutionResult!.Status);
        Assert.Equal("RULE-A-DOORS-FIRE-RATING", result.LoadResult.Rule!.Metadata.RuleId);
        Assert.NotNull(result.DoorParameterResult);
        Assert.Null(result.WindowParameterResult);
        Assert.Null(result.DoorNamingResult);
        Assert.Null(result.WindowNamingResult);
        Assert.Equal(100m, result.DoorParameterResult!.Summary!.CompliancePercentage);
        Assert.Contains("Doors FireRating Compliance Report", result.HtmlReport!.Html);
    }

    [Fact]
    public void Rule_B_validates_windows_with_acoustic_rating_only()
    {
        var result = _pipeline.Execute(CapabilityCompositionFixtureBuilder.CreateRequest(
            "RULE-B-WINDOWS-ACOUSTIC-RATING.bimrule",
            MvpValidationScenario.DemoPass));

        Assert.True(result.RuleValidationSucceeded);
        Assert.Equal(ExecutionStatus.Completed, result.ExecutionResult!.Status);
        Assert.Null(result.DoorParameterResult);
        Assert.NotNull(result.WindowParameterResult);
        Assert.Equal(100m, result.WindowParameterResult!.Summary!.CompliancePercentage);
        Assert.Contains("Windows AcousticRating Compliance Report", result.HtmlReport!.Html);
    }

    [Fact]
    public void Rule_C_validates_furniture_with_manufacturer_only()
    {
        var result = _pipeline.Execute(CapabilityCompositionFixtureBuilder.CreateRequest(
            "RULE-C-FURNITURE-MANUFACTURER.bimrule",
            MvpValidationScenario.FurniturePass));

        Assert.True(result.RuleValidationSucceeded);
        Assert.Equal(ExecutionStatus.Completed, result.ExecutionResult!.Status);
        Assert.Null(result.DoorParameterResult);
        Assert.Null(result.WindowParameterResult);
        Assert.Contains("Furniture Manufacturer Compliance Report", result.HtmlReport!.Html);
        Assert.NotNull(result.ReportOutput);
        Assert.DoesNotContain("Required parameter", result.HtmlReport.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Std_arc_openings_rule_still_executes_with_capability_configuration()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DemoPass));

        Assert.True(result.RuleValidationSucceeded);
        Assert.Equal("STD-ARC-OPENINGS-V01", result.LoadResult.Rule!.Metadata.RuleId);
        Assert.Equal(ExecutionStatus.Completed, result.ExecutionResult!.Status);
        Assert.NotNull(result.DoorParameterResult);
        Assert.NotNull(result.WindowParameterResult);
        Assert.NotNull(result.DoorNamingResult);
        Assert.NotNull(result.WindowNamingResult);
    }

    [Fact]
    public void Different_bimrules_produce_different_execution_behavior()
    {
        var ruleA = _pipeline.Execute(CapabilityCompositionFixtureBuilder.CreateRequest(
            "RULE-A-DOORS-FIRE-RATING.bimrule",
            MvpValidationScenario.DemoPass));
        var ruleB = _pipeline.Execute(CapabilityCompositionFixtureBuilder.CreateRequest(
            "RULE-B-WINDOWS-ACOUSTIC-RATING.bimrule",
            MvpValidationScenario.DemoPass));

        Assert.NotEqual(ruleA.JsonReport!.Json, ruleB.JsonReport!.Json);
        Assert.NotNull(ruleA.DoorParameterResult);
        Assert.Null(ruleA.WindowParameterResult);
        Assert.Null(ruleB.DoorParameterResult);
        Assert.NotNull(ruleB.WindowParameterResult);
    }

    [Fact]
    public void Execution_does_not_depend_on_rule_id_for_supported_configuration()
    {
        var result = _pipeline.Execute(CapabilityCompositionFixtureBuilder.CreateRequest(
            "RULE-A-DOORS-FIRE-RATING.bimrule",
            MvpValidationScenario.DoorPass));

        Assert.Equal("RULE-A-DOORS-FIRE-RATING", result.LoadResult.Rule!.Metadata.RuleId);
        Assert.Equal(ExecutionStatus.Completed, result.ExecutionResult!.Status);
        Assert.Equal(100m, result.DoorParameterResult!.Summary!.CompliancePercentage);
    }
}
