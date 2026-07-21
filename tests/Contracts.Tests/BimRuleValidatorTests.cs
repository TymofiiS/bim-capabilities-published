using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Validation;

namespace BIMCapabilities.Contracts.Tests;

public class BimRuleValidatorTests
{
    private readonly BimRuleValidator _validator = new();

    [Fact]
    public void Validate_passes_for_valid_bimrule()
    {
        var result = _validator.Validate(BimRuleTestData.CreateDemoRule());

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_detects_missing_rule_id()
    {
        var rule = BimRuleTestData.CreateDemoRule() with
        {
            Metadata = BimRuleTestData.CreateDemoRule().Metadata with { RuleId = string.Empty }
        };

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(BimRuleValidationDiagnosticCodes.RuleIdMissing, diagnostic.Code);
        Assert.Equal(ValidationSeverity.Error, diagnostic.Severity);
        Assert.Equal("metadata.ruleId", diagnostic.Location);
    }

    [Fact]
    public void Validate_detects_missing_version()
    {
        var metadata = BimRuleTestData.CreateDemoRule().Metadata with
        {
            RuleVersion = string.Empty,
            ContractVersion = string.Empty
        };

        var rule = BimRuleTestData.CreateDemoRule() with { Metadata = metadata };

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == BimRuleValidationDiagnosticCodes.RuleVersionMissing);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == BimRuleValidationDiagnosticCodes.ContractVersionMissing);
    }

    [Fact]
    public void Validate_detects_missing_engine_definitions()
    {
        var rule = BimRuleTestData.CreateDemoRule() with { Engines = [] };

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(BimRuleValidationDiagnosticCodes.EnginesEmpty, diagnostic.Code);
        Assert.Equal("engines", diagnostic.Location);
    }

    [Fact]
    public void Validate_detects_missing_execution_section()
    {
        var rule = BimRuleTestData.CreateDemoRule() with { Execution = null! };

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(BimRuleValidationDiagnosticCodes.ExecutionMissing, diagnostic.Code);
        Assert.Equal("execution", diagnostic.Location);
    }

    [Fact]
    public void Validate_detects_missing_report_section()
    {
        var rule = BimRuleTestData.CreateDemoRule() with { Report = null! };

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(BimRuleValidationDiagnosticCodes.ReportMissing, diagnostic.Code);
        Assert.Equal("report", diagnostic.Location);
    }

    [Fact]
    public void Validate_returns_multiple_validation_errors()
    {
        var rule = BimRuleTestData.CreateDemoRule() with
        {
            Metadata = BimRuleTestData.CreateDemoRule().Metadata with
            {
                RuleId = string.Empty,
                Name = string.Empty
            },
            Engines = [],
            Execution = null!,
            Report = null!
        };

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        Assert.True(result.Diagnostics.Count >= 5);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(ValidationSeverity.Error, diagnostic.Severity));
    }

    [Fact]
    public void Validate_diagnostics_include_code_severity_message_and_location()
    {
        var rule = BimRuleTestData.CreateDemoRule() with
        {
            Metadata = BimRuleTestData.CreateDemoRule().Metadata with { RuleId = "   " }
        };

        var result = _validator.Validate(rule);
        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Code));
        Assert.Equal(ValidationSeverity.Error, diagnostic.Severity);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Location));
    }

    [Fact]
    public void Validate_detects_null_rule_document()
    {
        var result = _validator.Validate(null);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(BimRuleValidationDiagnosticCodes.RuleMissing, diagnostic.Code);
    }
}
