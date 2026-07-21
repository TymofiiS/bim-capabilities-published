using BIMCapabilities.Contracts.Rules.Generation;
using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Versions;

namespace BIMCapabilities.Generation.Tests;

public class BimRuleGeneratorTests
{
    private readonly IBimRuleGenerator _generator = new BimRuleGenerator();

    [Fact]
    public void Generates_door_rules_from_natural_language()
    {
        var result = _generator.Generate(new BimRuleGenerationRequest
        {
            NaturalLanguagePrompt = BimRuleGenerationTestPrompts.DoorOnlyPrompt
        });

        Assert.True(result.Success);
        Assert.True(result.ValidationSucceeded);
        Assert.NotNull(result.Rule);
        Assert.Equal("STD-ARC-OPENINGS-V01.bimrule", result.OutputFileName);
        Assert.Contains("Doors", result.Report!.DetectedCategories);
        Assert.Contains("DR_", result.Report.DetectedNamingRules.First());
        Assert.Contains("FireRating", result.Report.DetectedParameters);
        Assert.Contains("RoomName", result.Report.DetectedParameters);
        Assert.Contains("Doors do not contain imported CAD", result.Report.DetectedComplianceRules);
    }

    [Fact]
    public void Generates_window_rules_from_natural_language()
    {
        var result = _generator.Generate(new BimRuleGenerationRequest
        {
            NaturalLanguagePrompt = BimRuleGenerationTestPrompts.WindowOnlyPrompt
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Contains("Windows", result.Report.DetectedCategories);
        Assert.Contains("WN_", result.Report.DetectedNamingRules.First());
        Assert.Contains("AcousticRating", result.Report.DetectedParameters);
        Assert.Contains("RoomName", result.Report.DetectedParameters);
    }

    [Fact]
    public void Generates_mixed_door_and_window_rules()
    {
        var result = _generator.Generate(new BimRuleGenerationRequest
        {
            NaturalLanguagePrompt = BimRuleGenerationTestPrompts.ArchitectureOpeningsPrompt
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal("STD-ARC-OPENINGS-V01", result.Report.GeneratedRuleName);
        Assert.Equal(["Doors", "Windows"], result.Report.DetectedCategories);
        Assert.Contains("FireRating", result.Report.DetectedParameters);
        Assert.Contains("AcousticRating", result.Report.DetectedParameters);
        Assert.Equal(2, result.Report.DetectedNamingRules.Count);
        Assert.Equal(3, result.Report.DetectedComplianceRules.Count);
    }

    [Fact]
    public void Generates_shared_parameter_file_path()
    {
        var result = _generator.Generate(new BimRuleGenerationRequest
        {
            NaturalLanguagePrompt = BimRuleGenerationTestPrompts.ArchitectureOpeningsPrompt
        });

        Assert.True(result.Success);
        Assert.Equal(@"D:\Company\SharedParameters.txt", result.Report!.SharedParameterFilePath);
        Assert.NotNull(result.Rule!.ExternalReferences);
        Assert.Single(result.Rule.ExternalReferences);
        Assert.Equal(@"D:\Company\SharedParameters.txt", result.Rule.ExternalReferences[0].Location);
    }

    [Fact]
    public void Generated_bimrule_passes_loader_and_validators()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "bim-generation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = _generator.Generate(new BimRuleGenerationRequest
            {
                NaturalLanguagePrompt = BimRuleGenerationTestPrompts.ArchitectureOpeningsPrompt,
                OutputDirectory = outputDirectory
            });

            Assert.True(result.Success);
            Assert.NotNull(result.OutputFilePath);
            Assert.True(File.Exists(result.OutputFilePath));

            var loadResult = new BimRuleLoader().Load(result.OutputFilePath);
            Assert.True(loadResult.Success);

            var structureResult = new BimRuleValidator().Validate(loadResult.Rule);
            var versionResult = new BimRuleVersionValidator().Validate(loadResult.Rule);
            var capabilityResult = new CapabilityCompatibilityValidator().Validate(loadResult.Rule);

            Assert.True(structureResult.IsValid);
            Assert.True(versionResult.IsValid);
            Assert.True(capabilityResult.IsValid);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Generates_interior_openings_rule()
    {
        var result = _generator.Generate(new BimRuleGenerationRequest
        {
            NaturalLanguagePrompt = BimRuleGenerationTestPrompts.InteriorOpeningsPrompt
        });

        Assert.True(result.Success);
        Assert.Equal("STD-INT-OPENINGS-V01", result.Report!.GeneratedRuleName);
        Assert.Contains("FinishType", result.Report.DetectedParameters);
    }

    [Fact]
    public void Generates_mep_equipment_rule()
    {
        var result = _generator.Generate(new BimRuleGenerationRequest
        {
            NaturalLanguagePrompt = BimRuleGenerationTestPrompts.MepEquipmentPrompt
        });

        Assert.True(result.Success);
        Assert.Equal("STD-MEP-EQUIPMENT-V01", result.Report!.GeneratedRuleName);
        Assert.Contains("Mechanical Equipment", result.Report.DetectedCategories);
        Assert.Contains("Manufacturer", result.Report.DetectedParameters);
        Assert.Contains("ModelNumber", result.Report.DetectedParameters);
    }

    [Fact]
    public void Generates_furniture_rules_from_natural_language()
    {
        var result = _generator.Generate(new BimRuleGenerationRequest
        {
            NaturalLanguagePrompt = BimRuleGenerationTestPrompts.FurniturePrompt
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Report);
        Assert.Equal("STD-ARC-FURNITURE-V01", result.Report.GeneratedRuleName);
        Assert.Contains("Furniture", result.Report.DetectedCategories);
        Assert.Contains("Manufacturer", result.Report.DetectedParameters);
        Assert.Contains("Furniture do not contain imported CAD", result.Report.DetectedComplianceRules);
        Assert.DoesNotContain(result.Report.DetectedNamingRules, rule => rule.Contains("FR_"));
    }

    [Fact]
    public void Generation_report_includes_required_sections()
    {
        var result = _generator.Generate(new BimRuleGenerationRequest
        {
            NaturalLanguagePrompt = BimRuleGenerationTestPrompts.ArchitectureOpeningsPrompt
        });

        Assert.NotNull(result.Report);
        Assert.NotEmpty(result.Report.DetectedCategories);
        Assert.NotEmpty(result.Report.DetectedParameters);
        Assert.NotEmpty(result.Report.DetectedNamingRules);
        Assert.NotEmpty(result.Report.DetectedComplianceRules);
        Assert.NotNull(result.Report.SharedParameterFilePath);
        Assert.Equal("STD-ARC-OPENINGS-V01", result.Report.GeneratedRuleName);
    }
}
