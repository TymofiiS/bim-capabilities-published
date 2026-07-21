using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Engines.Naming.Tests.Fixtures;
using BIMCapabilities.Engines.Naming.Write;

namespace BIMCapabilities.Engines.Naming.Tests;

public class NamingWriteRequestBuilderTests
{
    private readonly NamingWriteRequestBuilder _builder = new();

    [Fact]
    public void Builder_generates_rename_family_request_for_failed_door_finding()
    {
        var result = _builder.Build(NamingWriteRequestBuilderTestData.CreateDoorBuildRequest(
            NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                "family-101",
                "family",
                "Door_Single",
                "prefix",
                "MissingPrefix"),
            correctionIntents:
            [
                new NamingWriteCorrectionIntent
                {
                    ObjectId = "family-101",
                    ProposedName = "DR_SingleDoor"
                }
            ]));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.RenameFamily, request.RequestType);
        Assert.Equal("family-101", request.TargetObject.Id);
        Assert.Equal("Door_Single", request.Payload!["currentName"]);
        Assert.Equal("DR_SingleDoor", request.Payload["proposedName"]);
        Assert.Equal("DR_{Token}", request.Payload["namingRule"]);
        Assert.Equal(CorrelationId, request.CorrelationId);
    }

    [Fact]
    public void Builder_generates_rename_type_request_for_failed_type_finding()
    {
        var result = _builder.Build(NamingWriteRequestBuilderTestData.CreateDoorBuildRequest(
            NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                "family-type-101",
                "familyType",
                "HTL_Door_Invalid",
                "pattern",
                "InvalidPattern"),
            correctionIntents:
            [
                new NamingWriteCorrectionIntent
                {
                    ObjectId = "family-type-101",
                    ProposedName = "DR_SingleDoor900x2100"
                }
            ]));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.RenameType, request.RequestType);
        Assert.Equal("family-type-101", request.TargetObject.Id);
        Assert.Equal("HTL_Door_Invalid", request.Payload!["currentName"]);
        Assert.Equal("DR_SingleDoor900x2100", request.Payload["proposedName"]);
    }

    [Fact]
    public void Dr_prefix_scenario_generates_rename_family_request_with_door_naming_rule()
    {
        var result = _builder.Build(NamingWriteRequestBuilderTestData.CreateDoorBuildRequest(
            NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                "family-102",
                "family",
                "DR-",
                "pattern",
                "InvalidPattern")));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.RenameFamily, request.RequestType);
        Assert.Equal("DR_", request.Payload!["namingRule"].Split('{')[0]);
        Assert.StartsWith("DR_", request.Payload["proposedName"], StringComparison.Ordinal);
    }

    [Fact]
    public void Wn_prefix_scenario_generates_rename_family_request_with_window_naming_rule()
    {
        var result = _builder.Build(NamingWriteRequestBuilderTestData.CreateWindowBuildRequest(
            NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                "family-301",
                "family",
                "Window_01",
                "prefix",
                "MissingPrefix")));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.RenameFamily, request.RequestType);
        Assert.Equal("WN_{Token}", request.Payload!["namingRule"]);
        Assert.Equal("Window_01", request.Payload["currentName"]);
        Assert.Equal("WN_01", request.Payload["proposedName"]);
    }

    [Fact]
    public void Builder_generates_statistics_for_processed_findings_and_requests()
    {
        var result = _builder.Build(NamingWriteRequestBuilderTestData.CreateDoorBuildRequest(
            new NamingComplianceResult
            {
                EngineId = "naming.compliance",
                Findings =
                [
                    NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                        "family-101", "family", "Door_Single", "prefix", "MissingPrefix").Findings![0],
                    NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                        "family-type-101", "familyType", "HTL_Door_Invalid", "pattern", "InvalidPattern").Findings![0]
                ]
            },
            correctionIntents:
            [
                new NamingWriteCorrectionIntent { ObjectId = "family-101", ProposedName = "DR_SingleDoor" },
                new NamingWriteCorrectionIntent { ObjectId = "family-type-101", ProposedName = "DR_SingleDoor900x2100" }
            ]));

        Assert.Equal(2, result.Statistics!.FindingsProcessed);
        Assert.Equal(2, result.Statistics.RequestsGenerated);
        Assert.Equal(1, result.Statistics.RenameFamilyRequests);
        Assert.Equal(1, result.Statistics.RenameTypeRequests);
    }

    [Fact]
    public void Builder_skips_rename_when_prefix_fix_scope_is_none()
    {
        var buildRequest = NamingWriteRequestBuilderTestData.CreateDoorBuildRequest(
            NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                "family-101",
                "family",
                "Door_Single",
                "prefix",
                "MissingPrefix")) with
        {
            PrefixFixScope = PrefixFixScope.None
        };

        var result = _builder.Build(buildRequest);

        Assert.Empty(result.WriteRequests!);
    }

    [Fact]
    public void Builder_generates_diagnostics_for_request_generation()
    {
        var result = _builder.Build(NamingWriteRequestBuilderTestData.CreateDoorBuildRequest(
            NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                "family-101",
                "family",
                "Door_Single",
                "prefix",
                "MissingPrefix"),
            correctionIntents:
            [
                new NamingWriteCorrectionIntent
                {
                    ObjectId = "family-101",
                    ProposedName = "DR_SingleDoor"
                }
            ]));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "NamingWriteRequestBuilder.Started");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "NamingWriteRequestBuilder.RequestGenerated");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "NamingWriteRequestBuilder.Completed");
    }

    [Fact]
    public void Builder_produces_deterministic_write_requests()
    {
        var buildRequest = NamingWriteRequestBuilderTestData.CreateWindowBuildRequest(
            NamingWriteRequestBuilderTestData.CreateFailedFamilyFinding(
                "family-301",
                "family",
                "Window_01",
                "prefix",
                "MissingPrefix"));

        var first = _builder.Build(buildRequest);
        var second = _builder.Build(buildRequest);

        Assert.Equal(first.WriteRequests![0].RequestId, second.WriteRequests![0].RequestId);
        Assert.Equal(first.WriteRequests[0].Payload!["proposedName"], second.WriteRequests[0].Payload!["proposedName"]);
        Assert.Equal(first.Statistics!.RequestsGenerated, second.Statistics!.RequestsGenerated);
    }

    [Fact]
    public void Builder_deduplicates_multiple_failed_findings_for_same_object()
    {
        var result = _builder.Build(NamingWriteRequestBuilderTestData.CreateDoorBuildRequest(
            new NamingComplianceResult
            {
                EngineId = "naming.compliance",
                Findings =
                [
                    new NamingComplianceFinding
                    {
                        ValidationStage = "prefix",
                        ObjectId = "family-101",
                        ObjectKind = "family",
                        ObjectName = "Door_Single",
                        Passed = false,
                        Status = "MissingPrefix"
                    },
                    new NamingComplianceFinding
                    {
                        ValidationStage = "pattern",
                        ObjectId = "family-101",
                        ObjectKind = "family",
                        ObjectName = "Door_Single",
                        Passed = false,
                        Status = "InvalidPattern"
                    }
                ]
            },
            correctionIntents:
            [
                new NamingWriteCorrectionIntent
                {
                    ObjectId = "family-101",
                    ProposedName = "DR_SingleDoor"
                }
            ]));

        Assert.Single(result.WriteRequests!);
        Assert.Equal(1, result.Statistics!.RequestsGenerated);
    }

    [Fact]
    public void Naming_write_request_builder_does_not_reference_revit_assemblies()
    {
        var assembly = typeof(NamingWriteRequestBuilder).Assembly;
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal(["BIMCapabilities.Contracts"], referencedAssemblies.Where(name => name!.StartsWith("BIMCapabilities.", StringComparison.Ordinal)));
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Naming_write_request_builder_does_not_contain_execution_or_transaction_methods()
    {
        var methods = typeof(NamingWriteRequestBuilder).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

        Assert.All(methods, method =>
        {
            Assert.DoesNotContain("Execute", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Transaction", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rollback", method.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    private const string CorrelationId = NamingWriteRequestBuilderTestData.CorrelationId;
}
