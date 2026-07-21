using BIMCapabilities.Contracts.Rules.Loading;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Versions;

namespace BIMCapabilities.Contracts.Tests;

public class BimRuleEndToEndValidationTests
{
    private readonly BimRuleValidationPipeline _pipeline = new();

    [Fact]
    public void ValidRule_workflow_passes_full_pipeline()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("ValidRule.bimrule"));

        Assert.True(result.LoadSucceeded);
        Assert.True(result.StructureResult!.IsValid);
        Assert.True(result.VersionResult!.IsValid);
        Assert.True(result.CapabilityResult!.IsValid);
        Assert.True(result.IsFullyValid);
        Assert.Empty(result.GetAggregatedDiagnostics());
        Assert.Equal("STD-ARC-OPENINGS-V01", result.LoadResult.Rule!.Metadata.RuleId);
    }

    [Fact]
    public void MissingVersion_workflow_fails_structure_and_version_validation()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("MissingVersion.bimrule"));

        Assert.True(result.LoadSucceeded);
        Assert.False(result.StructureResult!.IsValid);
        Assert.False(result.VersionResult!.IsValid);
        Assert.True(result.CapabilityResult!.IsValid);
        Assert.False(result.IsFullyValid);

        Assert.Contains(
            result.StructureResult.Diagnostics,
            diagnostic => diagnostic.Code == BimRuleValidationDiagnosticCodes.ContractVersionMissing);
        Assert.Contains(
            result.StructureResult.Diagnostics,
            diagnostic => diagnostic.Code == BimRuleValidationDiagnosticCodes.RuleVersionMissing);
        Assert.Contains(
            result.VersionResult.Diagnostics,
            diagnostic => diagnostic.Code == VersionValidationDiagnosticCodes.ContractVersionMissing);
    }

    [Fact]
    public void MissingEngine_workflow_fails_structure_validation()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("MissingEngine.bimrule"));

        Assert.True(result.LoadSucceeded);
        Assert.False(result.StructureResult!.IsValid);
        Assert.True(result.VersionResult!.IsValid);
        Assert.True(result.CapabilityResult!.IsValid);
        Assert.False(result.IsFullyValid);

        var diagnostic = Assert.Single(result.StructureResult.Diagnostics);
        Assert.Equal(BimRuleValidationDiagnosticCodes.EnginesEmpty, diagnostic.Code);
        Assert.Equal("engines", diagnostic.Location);
    }

    [Fact]
    public void UnknownCapability_workflow_fails_capability_validation()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("UnknownCapability.bimrule"));

        Assert.True(result.LoadSucceeded);
        Assert.True(result.StructureResult!.IsValid);
        Assert.True(result.VersionResult!.IsValid);
        Assert.False(result.CapabilityResult!.IsValid);
        Assert.False(result.IsFullyValid);

        var diagnostic = Assert.Single(result.CapabilityResult.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.CapabilityUnknown, diagnostic.Code);
        Assert.Equal("naming.unknown.atom", diagnostic.ActualCapability);
    }

    [Fact]
    public void DeprecatedCapability_workflow_detects_deprecated_capability_warning()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("DeprecatedCapability.bimrule"));

        Assert.True(result.LoadSucceeded);
        Assert.True(result.StructureResult!.IsValid);
        Assert.True(result.VersionResult!.IsValid);
        Assert.True(result.CapabilityResult!.IsValid);
        Assert.True(result.IsFullyValid);

        var diagnostic = Assert.Single(result.CapabilityResult.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.CapabilityDeprecated, diagnostic.Code);
        Assert.Equal(ValidationSeverity.Warning, diagnostic.Severity);
        Assert.Equal("naming.prefix.legacy", diagnostic.ActualCapability);
    }

    [Fact]
    public void DuplicateCapability_workflow_fails_capability_validation()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("DuplicateCapability.bimrule"));

        Assert.True(result.LoadSucceeded);
        Assert.True(result.StructureResult!.IsValid);
        Assert.True(result.VersionResult!.IsValid);
        Assert.False(result.CapabilityResult!.IsValid);
        Assert.False(result.IsFullyValid);

        var diagnostic = Assert.Single(result.CapabilityResult.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.CapabilityDuplicate, diagnostic.Code);
        Assert.Equal("naming.prefix.validation", diagnostic.CapabilityName);
    }

    [Fact]
    public void MalformedRule_workflow_fails_at_load_stage()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("MalformedRule.bimrule"));

        Assert.False(result.LoadSucceeded);
        Assert.Null(result.StructureResult);
        Assert.Null(result.VersionResult);
        Assert.Null(result.CapabilityResult);
        Assert.False(result.IsFullyValid);

        var diagnostic = Assert.Single(result.LoadResult.Diagnostics);
        Assert.Equal(BimRuleLoadDiagnosticCodes.InvalidFormat, diagnostic.Code);
    }

    [Fact]
    public void EmptyRule_workflow_fails_at_load_stage()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("EmptyRule.bimrule"));

        Assert.False(result.LoadSucceeded);
        Assert.Null(result.StructureResult);
        Assert.Null(result.VersionResult);
        Assert.Null(result.CapabilityResult);
        Assert.False(result.IsFullyValid);

        var diagnostic = Assert.Single(result.LoadResult.Diagnostics);
        Assert.Equal(BimRuleLoadDiagnosticCodes.FileEmpty, diagnostic.Code);
    }

    [Fact]
    public void Pipeline_aggregates_diagnostics_deterministically()
    {
        var firstRun = _pipeline.Validate(BimRuleFixturePaths.Get("MissingVersion.bimrule")).GetAggregatedDiagnostics();
        var secondRun = _pipeline.Validate(BimRuleFixturePaths.Get("MissingVersion.bimrule")).GetAggregatedDiagnostics();

        Assert.Equal(firstRun.Count, secondRun.Count);
        for (var index = 0; index < firstRun.Count; index++)
        {
            Assert.Equal(firstRun[index].Stage, secondRun[index].Stage);
            Assert.Equal(firstRun[index].Code, secondRun[index].Code);
            Assert.Equal(firstRun[index].Message, secondRun[index].Message);
        }

        Assert.Contains(firstRun, diagnostic => diagnostic.Stage == "Structure");
        Assert.Contains(firstRun, diagnostic => diagnostic.Stage == "Version");
        Assert.True(firstRun.Count >= 3);
    }

    [Fact]
    public void Full_end_to_end_pipeline_executes_all_validators_together()
    {
        var result = _pipeline.Validate(BimRuleFixturePaths.Get("ValidRule.bimrule"));

        Assert.NotNull(result.LoadResult.Rule);
        Assert.NotNull(result.StructureResult);
        Assert.NotNull(result.VersionResult);
        Assert.NotNull(result.CapabilityResult);
        Assert.True(result.IsFullyValid);

        var aggregated = result.GetAggregatedDiagnostics();
        Assert.Empty(aggregated);
    }

    [Fact]
    public void All_fixtures_exist_in_test_project_only()
    {
        var fixtureNames = new[]
        {
            "ValidRule.bimrule",
            "MissingVersion.bimrule",
            "MissingEngine.bimrule",
            "UnknownCapability.bimrule",
            "DeprecatedCapability.bimrule",
            "DuplicateCapability.bimrule",
            "MalformedRule.bimrule",
            "EmptyRule.bimrule"
        };

        foreach (var fixtureName in fixtureNames)
        {
            var fixturePath = BimRuleFixturePaths.Get(fixtureName);
            Assert.True(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");
            Assert.Contains("Contracts.Tests", fixturePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
