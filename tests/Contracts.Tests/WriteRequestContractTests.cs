using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Contracts.Tests;

public class WriteRequestContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = WriteRequestSerialization.Options;

    [Fact]
    public void Write_request_contracts_are_data_only_types()
    {
        var writeTypes = typeof(WriteRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(WriteRequest).Namespace)
            .Where(type => type != typeof(WriteRequestSerialization))
            .Where(type => type != typeof(TransactionSerialization))
            .Where(type => type != typeof(IWriteRequest))
            .Where(type => type != typeof(ITransactionRequest))
            .Where(type => type != typeof(IRevitWriteAdapter))
            .Where(type => type != typeof(IWriteRequestExecutor))
            .Where(type => type != typeof(ITransactionExecutor))
            .Where(type => type != typeof(IWriteDiagnostics))
            .Where(type => type != typeof(IWriteResultCollector))
            .Where(type => !type.IsEnum)
            .Where(type => !type.Name.StartsWith("Transaction", StringComparison.Ordinal))
            .Where(type => !type.Name.EndsWith("Severity", StringComparison.Ordinal));

        Assert.All(writeTypes, type =>
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void WriteRequest_implements_IWriteRequest()
    {
        Assert.True(typeof(IWriteRequest).IsAssignableFrom(typeof(WriteRequest)));
    }

    [Fact]
    public void WriteRequest_can_be_constructed_with_required_properties()
    {
        var request = WriteRequestTestData.CreateParameterUpdateRequest();

        Assert.Equal("write-request-parameter-update-001", request.RequestId);
        Assert.Equal("family-001", request.TargetObject.Id);
        Assert.Equal("family", request.TargetObject.Kind);
        Assert.Equal(WriteRequestType.ParameterUpdate, request.RequestType);
        Assert.Equal("FireRating", request.Payload!["parameterName"]);
        Assert.Equal("60 min", request.Payload["value"]);
        Assert.Equal(WriteRequestTestData.RuleId, request.RuleId);
        Assert.Equal(WriteRequestTestData.CorrelationId, request.CorrelationId);
        Assert.Equal(WriteRequestTestData.RequestedAt, request.RequestedAt);
    }

    [Theory]
    [InlineData(WriteRequestType.ParameterCreate)]
    [InlineData(WriteRequestType.ParameterUpdate)]
    [InlineData(WriteRequestType.ParameterDelete)]
    [InlineData(WriteRequestType.RenameFamily)]
    [InlineData(WriteRequestType.RenameType)]
    [InlineData(WriteRequestType.Custom)]
    public void WriteRequestType_supports_required_request_types(WriteRequestType requestType)
    {
        var request = WriteRequestTestData.CreateParameterUpdateRequest() with
        {
            RequestType = requestType
        };

        Assert.Equal(requestType, request.RequestType);
    }

    [Fact]
    public void WriteRequestBatch_supports_multiple_ordered_requests()
    {
        var batch = WriteRequestTestData.CreateBatch();

        Assert.Equal("write-batch-001", batch.BatchId);
        Assert.Equal(3, batch.Requests.Count);
        Assert.Equal(1, batch.Requests[0].Order);
        Assert.Equal(2, batch.Requests[1].Order);
        Assert.Equal(3, batch.Requests[2].Order);
        Assert.Equal(WriteRequestType.ParameterUpdate, batch.Requests[0].RequestType);
        Assert.Equal(WriteRequestType.RenameFamily, batch.Requests[1].RequestType);
        Assert.Equal(WriteRequestType.RenameType, batch.Requests[2].RequestType);
        Assert.Equal("fix", batch.Metadata!["executionMode"]);
        Assert.Equal(WriteRequestTestData.CorrelationId, batch.CorrelationId);
    }

    [Fact]
    public void WriteRequestResult_supports_status_diagnostics_references_and_execution_metadata()
    {
        var result = WriteRequestTestData.CreateSucceededResult();

        Assert.Equal(WriteRequestStatus.Succeeded, result.Status);
        Assert.Single(result.Diagnostics!);
        Assert.Equal(2, result.RequestReferences!.Count);
        Assert.Equal("write-batch-001", result.ExecutionMetadata!.BatchId);
        Assert.Equal("revit.adapter.write", result.ExecutionMetadata.AdapterId);
    }

    [Fact]
    public void WriteRequestDiagnostic_supports_required_structure()
    {
        var diagnostic = WriteRequestTestData.CreateFailedResult().Diagnostics!.Single();

        Assert.Equal("WriteRequest.Failed", diagnostic.Code);
        Assert.Contains("not found", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(WriteRequestDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("write-request-parameter-update-001", diagnostic.RequestId);
        Assert.Equal("adapter:revit-write", diagnostic.Location);
        Assert.Equal("FireRating", diagnostic.Data!["parameterName"]);
    }

    [Fact]
    public void WriteRequest_supports_json_round_trip_serialization()
    {
        var original = WriteRequestTestData.CreateParameterUpdateRequest();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<WriteRequest>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.RequestId, roundTrip.RequestId);
        Assert.Equal(original.TargetObject.Id, roundTrip.TargetObject.Id);
        Assert.Equal(original.RequestType, roundTrip.RequestType);
        Assert.Equal(original.Payload!["parameterName"], roundTrip.Payload!["parameterName"]);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void WriteRequestBatch_supports_json_round_trip_serialization()
    {
        var original = WriteRequestTestData.CreateBatch();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<WriteRequestBatch>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.BatchId, roundTrip.BatchId);
        Assert.Equal(original.Requests.Count, roundTrip.Requests.Count);
        Assert.Equal(original.Requests[2].RequestType, roundTrip.Requests[2].RequestType);
        Assert.Equal(original.Metadata!["executionMode"], roundTrip.Metadata!["executionMode"]);
    }

    [Fact]
    public void WriteRequestResult_supports_json_round_trip_serialization()
    {
        var original = WriteRequestTestData.CreateSucceededResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<WriteRequestResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Status, roundTrip.Status);
        Assert.Equal(original.RequestReferences!.Count, roundTrip.RequestReferences!.Count);
        Assert.Equal(original.ExecutionMetadata!.AdapterId, roundTrip.ExecutionMetadata!.AdapterId);
        Assert.Equal(original.Diagnostics![0].Code, roundTrip.Diagnostics![0].Code);
    }

    [Fact]
    public void Write_request_contracts_do_not_reference_revit_assemblies()
    {
        var assembly = typeof(WriteRequest).Assembly;
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Write_request_namespace_does_not_define_execution_types()
    {
        var writeTypes = typeof(WriteRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(WriteRequest).Namespace)
            .Where(type => type.Name.StartsWith("Write", StringComparison.Ordinal));

        Assert.All(writeTypes, type =>
        {
            Assert.DoesNotContain("Executor", type.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Processor", type.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Write_request_contracts_can_represent_mvp_fix_scenarios()
    {
        var parameterUpdate = WriteRequestTestData.CreateParameterUpdateRequest();
        var renameFamily = WriteRequestTestData.CreateRenameFamilyRequest();
        var renameType = WriteRequestTestData.CreateRenameTypeRequest();

        Assert.Equal(WriteRequestType.ParameterUpdate, parameterUpdate.RequestType);
        Assert.Equal(WriteRequestType.RenameFamily, renameFamily.RequestType);
        Assert.Equal(WriteRequestType.RenameType, renameType.RequestType);
        Assert.Equal("DR_SingleDoor", renameFamily.Payload!["newName"]);
    }
}
