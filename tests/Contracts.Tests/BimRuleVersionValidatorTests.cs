using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Versions;

namespace BIMCapabilities.Contracts.Tests;

public class BimRuleVersionValidatorTests
{
    private readonly BimRuleVersionValidator _validator = new();

    [Fact]
    public void Validate_passes_for_supported_contract_version()
    {
        var rule = BimRuleTestData.CreateDemoRule();

        var result = _validator.Validate(rule);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_fails_for_unsupported_contract_version()
    {
        var rule = CreateRuleWithContractVersion("0.5");

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(VersionValidationDiagnosticCodes.UnsupportedVersion, diagnostic.Code);
        Assert.Equal(ValidationSeverity.Error, diagnostic.Severity);
        Assert.Equal("1.0", diagnostic.ExpectedVersion);
        Assert.Equal("0.5", diagnostic.ActualVersion);
    }

    [Fact]
    public void Validate_fails_for_missing_contract_version()
    {
        var rule = CreateRuleWithContractVersion(string.Empty);

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(VersionValidationDiagnosticCodes.ContractVersionMissing, diagnostic.Code);
        Assert.Equal("1.0", diagnostic.ExpectedVersion);
    }

    [Fact]
    public void Validate_fails_for_future_contract_version()
    {
        var rule = CreateRuleWithContractVersion("2.0");

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(VersionValidationDiagnosticCodes.FutureVersion, diagnostic.Code);
        Assert.Equal(ValidationSeverity.Error, diagnostic.Severity);
        Assert.Equal("1.0", diagnostic.ExpectedVersion);
        Assert.Equal("2.0", diagnostic.ActualVersion);
    }

    [Fact]
    public void Validate_returns_deprecated_version_diagnostic()
    {
        var rule = CreateRuleWithContractVersion("0.9");

        var result = _validator.Validate(rule);

        Assert.True(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(VersionValidationDiagnosticCodes.DeprecatedVersion, diagnostic.Code);
        Assert.Equal(ValidationSeverity.Warning, diagnostic.Severity);
        Assert.Equal("1.0", diagnostic.ExpectedVersion);
        Assert.Equal("0.9", diagnostic.ActualVersion);
    }

    [Fact]
    public void Validate_diagnostics_include_expected_and_actual_versions()
    {
        var rule = CreateRuleWithContractVersion("2.0");

        var result = _validator.Validate(rule);
        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Code));
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
        Assert.Equal("1.0", diagnostic.ExpectedVersion);
        Assert.Equal("2.0", diagnostic.ActualVersion);
    }

    [Fact]
    public void Validate_compares_versions_deterministically_for_future_versions()
    {
        var validator = new BimRuleVersionValidator(
            BimRuleSupportedVersionCatalog.Default,
            BimRuleSupportedVersionCatalog.CurrentSupportedContractVersion);

        var futureResult = validator.Validate(CreateRuleWithContractVersion("1.1"));
        var unsupportedResult = validator.Validate(CreateRuleWithContractVersion("0.8"));

        Assert.Equal(VersionValidationDiagnosticCodes.FutureVersion, Assert.Single(futureResult.Diagnostics).Code);
        Assert.Equal(VersionValidationDiagnosticCodes.UnsupportedVersion, Assert.Single(unsupportedResult.Diagnostics).Code);
    }

    [Fact]
    public void Validate_fails_for_null_rule_contract_version()
    {
        var result = _validator.Validate(null);

        Assert.False(result.IsValid);
        Assert.Equal(VersionValidationDiagnosticCodes.ContractVersionMissing, Assert.Single(result.Diagnostics).Code);
    }

    private static BimRule CreateRuleWithContractVersion(string contractVersion)
    {
        return BimRuleTestData.CreateDemoRule() with
        {
            Metadata = BimRuleTestData.CreateDemoRule().Metadata with
            {
                ContractVersion = contractVersion
            }
        };
    }
}
