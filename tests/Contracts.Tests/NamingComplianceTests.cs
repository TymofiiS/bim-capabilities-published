using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Naming.Compliance;

namespace BIMCapabilities.Contracts.Tests;

public class NamingComplianceTests
{
    [Fact]
    public void Naming_compliance_contracts_are_data_only_types()
    {
        var complianceTypes = typeof(ComplianceContracts.NamingComplianceRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ComplianceContracts.NamingComplianceRequest).Namespace);

        Assert.All(complianceTypes, type =>
        {
            if (type == typeof(ComplianceContracts.INamingComplianceEngine))
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
    public void NamingComplianceRequest_and_result_can_be_constructed()
    {
        var request = new ComplianceContracts.NamingComplianceRequest
        {
            TargetSet = NamingEngineTestData.CreateDoorTargetSet(),
            RequiredPrefixes = [NamingEngineTestData.DoorPrefix],
            PatternRule = new NamingPatternRule
            {
                TokenizedPattern = "DR_{Token}",
                RegularExpression = @"^DR_[A-Za-z][A-Za-z0-9]*$"
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-naming-compliance-001"
        };

        var result = new ComplianceContracts.NamingComplianceResult
        {
            EngineId = "naming.compliance",
            Findings = [],
            Evidence = [],
            Statistics = new ComplianceContracts.NamingComplianceStatistics
            {
                ObjectsChecked = 0,
                ObjectsPassed = 0,
                ObjectsFailed = 0
            },
            Summary = new ComplianceContracts.NamingComplianceSummary
            {
                ObjectsChecked = 0,
                PassedChecks = 0,
                FailedChecks = 0,
                CompliancePercentage = 100m,
                NamingViolations = 0
            },
            Diagnostics =
            [
                new NamingEngineDiagnostic
                {
                    Code = "NamingCompliance.Completed",
                    Message = "Compliance evaluation completed.",
                    Severity = NamingEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal([NamingEngineTestData.DoorPrefix], request.RequiredPrefixes);
        Assert.Equal("naming.compliance", result.EngineId);
        Assert.Equal(100m, result.Summary!.CompliancePercentage);
    }

    [Fact]
    public void NamingComplianceSummary_supports_required_metrics()
    {
        var summary = new ComplianceContracts.NamingComplianceSummary
        {
            ObjectsChecked = 4,
            PassedChecks = 3,
            FailedChecks = 1,
            CompliancePercentage = 75m,
            NamingViolations = 1
        };

        Assert.Equal(75m, summary.CompliancePercentage);
        Assert.Equal(1, summary.NamingViolations);
    }

    [Fact]
    public void NamingComplianceRequest_supports_json_round_trip_serialization()
    {
        var original = new ComplianceContracts.NamingComplianceRequest
        {
            TargetSet = NamingEngineTestData.CreateDoorTargetSet(),
            RequiredPrefixes = [NamingEngineTestData.WindowPrefix],
            PatternRule = new NamingPatternRule
            {
                TokenizedPattern = "WN_{Token}"
            },
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-naming-compliance-001"
        };

        var json = JsonSerializer.Serialize(original, NamingEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<ComplianceContracts.NamingComplianceRequest>(
            json,
            NamingEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RequiredPrefixes, roundTrip.RequiredPrefixes);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(original.PatternRule!.TokenizedPattern, roundTrip.PatternRule!.TokenizedPattern);
    }

    [Fact]
    public void INamingComplianceEngine_defines_evaluation_contract()
    {
        var method = Assert.Single(
            typeof(ComplianceContracts.INamingComplianceEngine).GetMethods(),
            candidate => candidate.Name == "Evaluate");

        Assert.Equal(typeof(ComplianceContracts.NamingComplianceResult), method.ReturnType);
        Assert.Equal(typeof(ComplianceContracts.NamingComplianceRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Naming_compliance_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ComplianceContracts.NamingComplianceRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
