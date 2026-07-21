using System.Reflection;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using BIMCapabilities.Contracts.Engines.Parameter.Write;
using BIMCapabilities.Engines.Parameter.Tests.Fixtures;
using BIMCapabilities.Engines.Parameter.Write;

namespace BIMCapabilities.Engines.Parameter.Tests;

public class ParameterWriteRequestBuilderTests
{
    private readonly ParameterWriteRequestBuilder _builder = new();

    [Fact]
    public void Builder_generates_parameter_create_request_for_missing_parameter_finding()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateMissingParameterComplianceResult("FireRating")));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterCreate, request.RequestType);
        Assert.Equal("family-001", request.TargetObject.Id);
        Assert.Equal("FireRating", request.Payload!["parameterName"]);
        Assert.Equal("60 min", request.Payload["requestedValue"]);
        Assert.Equal(FireRatingGuid, request.Payload["parameterDefinitionGuid"]);
        Assert.Equal(CorrelationId, request.CorrelationId);
        Assert.Equal("false", request.Payload["parameterIsInstance"]);
    }

    [Fact]
    public void Builder_uses_instance_binding_when_rule_configuration_requests_it()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateMissingParameterComplianceResult("FireRating"),
            parameterBindings: new Dictionary<string, bool> { ["FireRating"] = true }));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal("true", request.Payload!["parameterIsInstance"]);
    }

    [Fact]
    public void Builder_uses_rule_configured_parameter_defaults_when_present()
    {
        var buildRequest = ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateMissingParameterComplianceResult("FireRating"));
        buildRequest = buildRequest with
        {
            ParameterDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FireRating"] = "EI60"
            }
        };

        var result = _builder.Build(buildRequest);
        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal("EI60", request.Payload!["requestedValue"]);
    }

    [Fact]
    public void Builder_uses_parameter_fill_rule_from_family_type_name()
    {
        var buildRequest = ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            new ParameterComplianceResult
            {
                EngineId = "parameter.compliance",
                Findings =
                [
                    new ParameterComplianceFinding
                    {
                        ValidationStage = "value",
                        ObjectId = "family-type-001",
                        ObjectKind = "familyType",
                        ObjectName = "DR_900x2100",
                        ParameterName = "Model",
                        Passed = false,
                        Status = "MissingValue",
                        Message = "Parameter 'Model' is missing a required value."
                    }
                ]
            },
            new ParameterTargetSet
            {
                TargetSetId = "target-set-doors-001",
                TargetTypes =
                [
                    new NormalizedFamilyType
                    {
                        Identity = new NormalizedIdentifier
                        {
                            Id = "family-type-001",
                            Kind = "familyType",
                            Scope = "project-document"
                        },
                        Name = "DR_900x2100"
                    }
                ]
            }) with
        {
            ParameterFillRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Model"] = "from:FamilyTypeName"
            }
        };

        var result = _builder.Build(buildRequest);
        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterUpdate, request.RequestType);
        Assert.Equal("DR_900x2100", request.Payload!["requestedValue"]);
    }

    [Fact]
    public void Builder_resolves_family_type_name_fill_rule_per_type()
    {
        var buildRequest = ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            new ParameterComplianceResult
            {
                EngineId = "parameter.compliance",
                Findings =
                [
                    new ParameterComplianceFinding
                    {
                        ValidationStage = "value",
                        ObjectId = "family-type-001",
                        ObjectKind = "familyType",
                        ObjectName = "750 x 2100mm",
                        ParameterName = "Model",
                        Passed = false,
                        Status = "MissingValue",
                        Message = "Parameter 'Model' is missing a required value."
                    },
                    new ParameterComplianceFinding
                    {
                        ValidationStage = "value",
                        ObjectId = "family-type-002",
                        ObjectKind = "familyType",
                        ObjectName = "900 x 2100mm",
                        ParameterName = "Model",
                        Passed = false,
                        Status = "MissingValue",
                        Message = "Parameter 'Model' is missing a required value."
                    }
                ]
            },
            new ParameterTargetSet
            {
                TargetSetId = "target-set-doors-001",
                TargetTypes =
                [
                    new NormalizedFamilyType
                    {
                        Identity = new NormalizedIdentifier
                        {
                            Id = "family-type-001",
                            Kind = "familyType",
                            Scope = "project-document"
                        },
                        Name = "750 x 2100mm"
                    },
                    new NormalizedFamilyType
                    {
                        Identity = new NormalizedIdentifier
                        {
                            Id = "family-type-002",
                            Kind = "familyType",
                            Scope = "project-document"
                        },
                        Name = "900 x 2100mm"
                    }
                ]
            }) with
        {
            ParameterFillRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Model"] = "from:FamilyTypeName"
            }
        };

        var result = _builder.Build(buildRequest);

        Assert.Equal(2, result.WriteRequests!.Count);
        Assert.Contains(result.WriteRequests, request =>
            request.TargetObject.Id == "family-type-001"
            && request.Payload!["requestedValue"] == "750 x 2100mm");
        Assert.Contains(result.WriteRequests, request =>
            request.TargetObject.Id == "family-type-002"
            && request.Payload!["requestedValue"] == "900 x 2100mm");
    }

    [Fact]
    public void Builder_generates_parameter_update_request_for_invalid_value_finding()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateInvalidValueComplianceResult("FireRating")));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterUpdate, request.RequestType);
        Assert.Equal("InvalidValue", request.Payload!["findingStatus"]);
        Assert.Equal("60 min", request.Payload["requestedValue"]);
    }

    [Fact]
    public void Builder_maps_instance_value_finding_to_family_target_for_fix()
    {
        var buildRequest = ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateInstanceMissingValueComplianceResult(
                "MY_Room",
                "1663918",
                "M_Window-Fixed"),
            ParameterWriteRequestBuilderTestData.CreateWindowTargetSetWithInstance("1663918"),
            parameterBindings: new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["MY_Room"] = true
            }) with
        {
            ParameterDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MY_Room"] = "TBD"
            }
        };

        var result = _builder.Build(buildRequest);

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterUpdate, request.RequestType);
        Assert.Equal("family-window-001", request.TargetObject.Id);
        Assert.Equal("true", request.Payload!["parameterIsInstance"]);
        Assert.Equal("TBD", request.Payload["requestedValue"]);
    }

    [Fact]
    public void Builder_generates_parameter_delete_request_from_correction_intent()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            new ParameterComplianceResult
            {
                EngineId = "parameter.compliance",
                Findings = []
            },
            correctionIntents:
            [
                new ParameterWriteCorrectionIntent
                {
                    ParameterName = "ObsoleteParameter",
                    ObjectId = "family-001",
                    RequestedAction = WriteRequestType.ParameterDelete
                }
            ]));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterDelete, request.RequestType);
        Assert.Equal("ObsoleteParameter", request.Payload!["parameterName"]);
    }

    [Fact]
    public void RoomName_scenario_generates_create_request_with_mvp_defaults()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateMissingParameterComplianceResult("RoomName")));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterCreate, request.RequestType);
        Assert.Equal("RoomName", request.Payload!["parameterName"]);
        Assert.Equal("Lobby", request.Payload["requestedValue"]);
        Assert.Equal(RoomNameGuid, request.Payload["parameterDefinitionGuid"]);
    }

    [Fact]
    public void FireRating_scenario_generates_update_request_with_shared_definition()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateInvalidValueComplianceResult("FireRating")));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal("FireRating", request.Payload!["parameterName"]);
        Assert.Equal(FireRatingGuid, request.Payload["parameterDefinitionGuid"]);
        Assert.Equal("TEXT", request.Payload["parameterDataType"]);
    }

    [Fact]
    public void AcousticRating_scenario_generates_create_request_for_missing_shared_parameter()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateMissingSharedParameterComplianceResult("AcousticRating")));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterCreate, request.RequestType);
        Assert.Equal("AcousticRating", request.Payload!["parameterName"]);
        Assert.Equal("45 dB", request.Payload["requestedValue"]);
        Assert.Equal(AcousticRatingGuid, request.Payload["parameterDefinitionGuid"]);
    }

    [Fact]
    public void Manufacturer_scenario_generates_update_request_with_requested_value()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateInvalidValueComplianceResult("Manufacturer"),
            correctionIntents:
            [
                new ParameterWriteCorrectionIntent
                {
                    ParameterName = "Manufacturer",
                    ObjectId = "family-001",
                    RequestedValue = "HTL Components Ltd"
                }
            ]));

        var request = Assert.Single(result.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterUpdate, request.RequestType);
        Assert.Equal("HTL Components Ltd", request.Payload!["requestedValue"]);
        Assert.Equal(ManufacturerGuid, request.Payload["parameterDefinitionGuid"]);
    }

    [Fact]
    public void Builder_generates_statistics_for_processed_findings_and_requests()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            new ParameterComplianceResult
            {
                EngineId = "parameter.compliance",
                Findings =
                [
                    ParameterWriteRequestBuilderTestData.CreateMissingParameterComplianceResult("FireRating").Findings![0],
                    ParameterWriteRequestBuilderTestData.CreateInvalidValueComplianceResult("RoomName").Findings![0]
                ]
            }));

        Assert.Equal(2, result.Statistics!.FindingsProcessed);
        Assert.Equal(2, result.Statistics.RequestsGenerated);
        Assert.Equal(1, result.Statistics.CreateRequests);
        Assert.Equal(1, result.Statistics.UpdateRequests);
        Assert.Equal(0, result.Statistics.DeleteRequests);
    }

    [Fact]
    public void Builder_generates_diagnostics_for_request_generation()
    {
        var result = _builder.Build(ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateMissingParameterComplianceResult("FireRating")));

        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterWriteRequestBuilder.Started");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterWriteRequestBuilder.RequestGenerated");
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "ParameterWriteRequestBuilder.Completed");
    }

    [Fact]
    public void Builder_produces_deterministic_write_requests()
    {
        var buildRequest = ParameterWriteRequestBuilderTestData.CreateBuildRequest(
            ParameterWriteRequestBuilderTestData.CreateMissingParameterComplianceResult("FireRating"));

        var first = _builder.Build(buildRequest);
        var second = _builder.Build(buildRequest);

        Assert.Equal(
            first.WriteRequests![0].RequestId,
            second.WriteRequests![0].RequestId);
        Assert.Equal(
            first.WriteRequests[0].Payload!["requestedValue"],
            second.WriteRequests[0].Payload!["requestedValue"]);
        Assert.Equal(
            first.Statistics!.RequestsGenerated,
            second.Statistics!.RequestsGenerated);
    }

    [Fact]
    public void Parameter_write_request_builder_does_not_reference_revit_assemblies()
    {
        var assembly = typeof(ParameterWriteRequestBuilder).Assembly;
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal(["BIMCapabilities.Contracts"], referencedAssemblies.Where(name => name!.StartsWith("BIMCapabilities.", StringComparison.Ordinal)));
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Parameter_write_request_builder_does_not_contain_execution_or_transaction_methods()
    {
        var methods = typeof(ParameterWriteRequestBuilder).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

        Assert.All(methods, method =>
        {
            Assert.DoesNotContain("Execute", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Transaction", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rollback", method.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    private const string CorrelationId = ParameterWriteRequestBuilderTestData.CorrelationId;
    private const string FireRatingGuid = ParameterWriteRequestBuilderTestData.FireRatingGuid;
    private const string RoomNameGuid = ParameterWriteRequestBuilderTestData.RoomNameGuid;
    private const string AcousticRatingGuid = ParameterWriteRequestBuilderTestData.AcousticRatingGuid;
    private const string ManufacturerGuid = ParameterWriteRequestBuilderTestData.ManufacturerGuid;
}
