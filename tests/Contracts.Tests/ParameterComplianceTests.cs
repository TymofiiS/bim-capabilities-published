using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Engines.Parameter.Value;
using BIMCapabilities.Contracts.Evidence;
using ComplianceContracts = BIMCapabilities.Contracts.Engines.Parameter.Compliance;

namespace BIMCapabilities.Contracts.Tests;

public class ParameterComplianceTests
{
    [Fact]
    public void Parameter_compliance_contracts_are_data_only_types()
    {
        var complianceTypes = typeof(ComplianceContracts.ParameterComplianceRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ComplianceContracts.ParameterComplianceRequest).Namespace);

        Assert.All(complianceTypes, type =>
        {
            if (type == typeof(ComplianceContracts.IParameterComplianceEngine))
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
    public void ParameterComplianceRequest_and_result_can_be_constructed()
    {
        var request = new ComplianceContracts.ParameterComplianceRequest
        {
            TargetSet = ParameterEngineTestData.CreateDoorTargetSet(),
            SharedParameterFile = ParameterEngineTestData.CreateSharedParameterFileReference(),
            RequiredParameterNames = ["FireRating", "RoomName", "Manufacturer"],
            SharedParameterNamesToValidate = ["FireRating", "Manufacturer"],
            ValueRules =
            [
                new ParameterValueRule
                {
                    ParameterName = "FireRating",
                    RequiredValue = true,
                    Severity = EvidenceSeverity.Error
                }
            ],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-compliance-001"
        };

        var result = new ComplianceContracts.ParameterComplianceResult
        {
            EngineId = "parameter.compliance",
            Findings = [],
            Evidence = [],
            Statistics = new ComplianceContracts.ParameterComplianceStatistics
            {
                ObjectsChecked = 0,
                ObjectsPassed = 0,
                ObjectsFailed = 0,
                ParametersChecked = 0
            },
            Summary = new ComplianceContracts.ParameterComplianceSummary
            {
                ObjectsChecked = 0,
                ParametersChecked = 0,
                PassedChecks = 0,
                FailedChecks = 0,
                CompliancePercentage = 100m
            },
            Diagnostics =
            [
                new ParameterEngineDiagnostic
                {
                    Code = "ParameterCompliance.Completed",
                    Message = "Compliance evaluation completed.",
                    Severity = ParameterEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal(3, request.RequiredParameterNames!.Count);
        Assert.Equal("parameter.compliance", result.EngineId);
        Assert.Equal(100m, result.Summary!.CompliancePercentage);
    }

    [Fact]
    public void ParameterComplianceSummary_supports_required_metrics()
    {
        var summary = new ComplianceContracts.ParameterComplianceSummary
        {
            ObjectsChecked = 4,
            ParametersChecked = 12,
            PassedChecks = 9,
            FailedChecks = 3,
            CompliancePercentage = 75m
        };

        Assert.Equal(75m, summary.CompliancePercentage);
        Assert.Equal(9, summary.PassedChecks);
        Assert.Equal(3, summary.FailedChecks);
    }

    [Fact]
    public void ParameterComplianceRequest_supports_json_round_trip_serialization()
    {
        var original = new ComplianceContracts.ParameterComplianceRequest
        {
            TargetSet = ParameterEngineTestData.CreateDoorTargetSet(),
            SharedParameterFile = ParameterEngineTestData.CreateSharedParameterFileReference(),
            RequiredParameterNames = ["Manufacturer"],
            ValueRules =
            [
                new ParameterValueRule
                {
                    ParameterName = "Manufacturer",
                    RequiredValue = true
                }
            ],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-parameter-compliance-001"
        };

        var json = JsonSerializer.Serialize(original, ParameterEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<ComplianceContracts.ParameterComplianceRequest>(
            json,
            ParameterEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RequiredParameterNames, roundTrip.RequiredParameterNames);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(original.SharedParameterFile!.FilePath, roundTrip.SharedParameterFile!.FilePath);
    }

    [Fact]
    public void IParameterComplianceEngine_defines_evaluation_contract()
    {
        var method = Assert.Single(
            typeof(ComplianceContracts.IParameterComplianceEngine).GetMethods(),
            candidate => candidate.Name == "Evaluate");

        Assert.Equal(typeof(ComplianceContracts.ParameterComplianceResult), method.ReturnType);
        Assert.Equal(typeof(ComplianceContracts.ParameterComplianceRequest), method.GetParameters()[0].ParameterType);
        Assert.Single(method.GetParameters());
    }

    [Fact]
    public void Parameter_compliance_contracts_do_not_reference_revit_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ComplianceContracts.ParameterComplianceRequest).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
