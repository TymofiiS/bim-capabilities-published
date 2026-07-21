using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;

namespace BIMCapabilities.Contracts.Tests;

public class BimRuleConfigurationValidatorTests
{
    private readonly BimRuleConfigurationValidator _validator = new();

    [Fact]
    public void Validate_passes_for_documented_literal_defaults()
    {
        var rule = CreateParameterRule(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Doors.parameters"] = "FireRating",
            ["Doors.parameterDefaults"] = "FireRating=EI60",
            ["Doors.parameterBinding"] = "FireRating=type"
        });

        var result = _validator.Validate(rule);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_accepts_parameter_fill_rules()
    {
        var rule = CreateParameterRule(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Doors.parameters"] = "Model",
            ["Doors.parameterFillRules"] = "Model=from:FamilyTypeName",
            ["Doors.parameterBinding"] = "Model=type"
        });

        var result = _validator.Validate(rule);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_rejects_type_name_placeholder_in_parameter_defaults()
    {
        var rule = CreateParameterRule(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Doors.parameters"] = "Model",
            ["Doors.parameterDefaults"] = "Model={TypeName}",
            ["Doors.parameterBinding"] = "Model=type"
        });

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.ConfigurationPlaceholderForbidden, diagnostic.Code);
        Assert.Contains("{TypeName}", diagnostic.ActualCapability, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_rejects_unknown_configuration_keys()
    {
        var rule = CreateParameterRule(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Doors.parameters"] = "Model",
            ["Doors.copyTypeNameToModel"] = "true"
        });

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.ConfigurationKeyUnknown, diagnostic.Code);
        Assert.Equal("Doors.copyTypeNameToModel", diagnostic.ActualCapability);
    }

    [Fact]
    public void Validate_rejects_invalid_parameter_binding_values()
    {
        var rule = CreateParameterRule(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Doors.parameters"] = "Model",
            ["Doors.parameterBinding"] = "Model=copy-from-type-name"
        });

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.ConfigurationValueInvalid, diagnostic.Code);
    }

    [Fact]
    public void ConfigurationKeyMatches_supports_category_prefixed_keys()
    {
        Assert.True(BimRuleConfigurationValidator.ConfigurationKeyMatches("Doors.parameterDefaults", "{Category}.parameterDefaults"));
        Assert.True(BimRuleConfigurationValidator.ConfigurationKeyMatches("Doors.parameterFillRules", "{Category}.parameterFillRules"));
        Assert.True(BimRuleConfigurationValidator.ConfigurationKeyMatches("Windows.prefixFix", "{Category}.prefixFix"));
        Assert.True(BimRuleConfigurationValidator.ConfigurationKeyMatches("Windows.prefix", "{Category}.prefix"));
        Assert.False(BimRuleConfigurationValidator.ConfigurationKeyMatches("parameterDefaults", "{Category}.parameterDefaults"));
    }

    private static BimRule CreateParameterRule(IReadOnlyDictionary<string, string> configuration)
    {
        return new BimRule
        {
            Metadata = new BimRuleMetadata
            {
                RuleId = "STD-ARC-DOORS-V01",
                Name = "STD-ARC-DOORS-V01",
                RuleVersion = "V01",
                ContractVersion = "1.0",
                Description = "Test rule",
                Domain = "Doors",
                Status = "Approved",
                Author = "Test",
                CreatedAt = DateTimeOffset.UtcNow
            },
            Engines =
            [
                new BimRuleEngine
                {
                    EngineId = "parameter-engine",
                    Order = 1,
                    Capabilities =
                    [
                        new BimRuleCapabilityReference
                        {
                            AtomId = "parameter.existence",
                            Configuration = configuration
                        }
                    ]
                },
                new BimRuleEngine
                {
                    EngineId = "report-engine",
                    Order = 2,
                    Capabilities =
                    [
                        new BimRuleCapabilityReference { AtomId = "report.compliance" }
                    ]
                }
            ],
            Execution = new BimRuleExecution
            {
                TargetPlatform = "Revit",
                ExecutionMode = "Validation",
                ValidationEnabled = true,
                FixEnabled = false,
                DryRun = false,
                RequireUserApprovalBeforeModification = false,
                FailureBehavior = "StopOnFirstUnrecoverableFailure"
            },
            Report = new BimRuleReport
            {
                GenerateHtmlReport = true,
                GenerateJsonReport = true,
                IncludeEvidence = true,
                ReportTitle = "Test",
                ComplianceSummaryProfile = "Compliance",
                ResultGrouping = "Engine"
            }
        };
    }
}
