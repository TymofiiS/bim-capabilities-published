using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Naming;
using PatternContracts = BIMCapabilities.Contracts.Engines.Naming.Pattern;

namespace BIMCapabilities.Contracts.Tests;

public class NamingPatternValidationTests
{
    [Fact]
    public void Naming_pattern_contracts_are_data_only_types()
    {
        var patternTypes = typeof(PatternContracts.NamingPatternValidationRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(PatternContracts.NamingPatternValidationRequest).Namespace);

        Assert.All(patternTypes, type =>
        {
            if (type == typeof(PatternContracts.INamingPatternValidationAtom))
            {
                return;
            }

            if (type.IsEnum)
            {
                return;
            }

            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void NamingPatternValidationRequest_and_result_can_be_constructed()
    {
        var request = new PatternContracts.NamingPatternValidationRequest
        {
            TargetSet = NamingEngineTestData.CreateDoorTargetSet(),
            Rule = new PatternContracts.NamingPatternRule
            {
                TokenizedPattern = "DR_{Token}",
                RegularExpression = @"^DR_[A-Za-z][A-Za-z0-9]*$",
                AllowedCharacters = "A-Za-z0-9_",
                ForbiddenCharacters = [" ", "-"],
                CaseRule = NamingCaseRule.PascalCase
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-naming-pattern-001"
        };

        var result = new PatternContracts.NamingPatternValidationResult
        {
            AtomId = "naming.validation.pattern",
            Findings = [],
            Evidence = [],
            Statistics = new PatternContracts.NamingPatternValidationStatistics
            {
                ObjectsChecked = 0,
                ObjectsPassed = 0,
                ObjectsFailed = 0,
                PatternViolations = 0,
                InvalidCharacterViolations = 0,
                LengthViolations = 0
            },
            Diagnostics =
            [
                new NamingEngineDiagnostic
                {
                    Code = "NamingPatternValidation.Completed",
                    Message = "Validation completed.",
                    Severity = NamingEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal("DR_{Token}", request.Rule.TokenizedPattern);
        Assert.Equal("naming.validation.pattern", result.AtomId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void NamingPatternRule_supports_all_validation_constraints()
    {
        var rule = new PatternContracts.NamingPatternRule
        {
            RegularExpression = @"^DR_[A-Za-z0-9]+$",
            TokenizedPattern = "DR_{Token}",
            MinimumLength = 4,
            MaximumLength = 64,
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"],
            CaseRule = NamingCaseRule.PascalCase,
            AllowNumericTokenStart = true
        };

        Assert.Equal(4, rule.MinimumLength);
        Assert.True(rule.AllowNumericTokenStart);
        Assert.Equal([" ", "-"], rule.ForbiddenCharacters);
    }

    [Fact]
    public void NamingPatternValidationRequest_supports_json_round_trip_serialization()
    {
        var original = new PatternContracts.NamingPatternValidationRequest
        {
            TargetSet = NamingEngineTestData.CreateDoorTargetSet(),
            Rule = new PatternContracts.NamingPatternRule
            {
                TokenizedPattern = "DR_{Token}",
                RegularExpression = @"^DR_[A-Za-z][A-Za-z0-9]*$",
                MinimumLength = 4
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-naming-pattern-001"
        };

        var json = JsonSerializer.Serialize(original, NamingEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<PatternContracts.NamingPatternValidationRequest>(
            json,
            NamingEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Rule.TokenizedPattern, roundTrip.Rule.TokenizedPattern);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void INamingPatternValidationAtom_defines_validation_contract()
    {
        var method = Assert.Single(
            typeof(PatternContracts.INamingPatternValidationAtom).GetMethods(),
            candidate => candidate.Name == "Validate");

        Assert.Equal(typeof(PatternContracts.NamingPatternValidationResult), method.ReturnType);
        Assert.Equal(typeof(PatternContracts.NamingPatternValidationRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Naming_pattern_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(PatternContracts.NamingPatternValidationRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
