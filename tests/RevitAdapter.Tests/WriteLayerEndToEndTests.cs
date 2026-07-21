using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Adapters.Revit.Tests.Fixtures;
using BIMCapabilities.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class WriteLayerEndToEndTests
{
    private readonly RevitWriteAdapter _adapter = new();

    [Fact]
    public void Parameter_workflow_generates_create_request_for_missing_fire_rating()
    {
        var result = WriteLayerWorkflowSupport.RunParameterWorkflow(
            MissingFireRatingFixture.CreateBuildRequest(),
            _adapter);

        var request = Assert.Single(result.BuildResult.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterCreate, request.RequestType);
        Assert.Equal("FireRating", request.Payload!["parameterName"]);
        Assert.Equal("60 min", request.Payload["requestedValue"]);
        Assert.Equal(CorrelationId, request.CorrelationId);
        Assert.Equal(WriteRequestStatus.NotExecuted, result.WriteLayerResult.Status);
        Assert.Equal(TransactionStatus.Pending, result.TransactionLayerResult.Status);
    }

    [Fact]
    public void Parameter_workflow_generates_create_request_for_missing_room_name()
    {
        var result = WriteLayerWorkflowSupport.RunParameterWorkflow(
            MissingRoomNameFixture.CreateBuildRequest(),
            _adapter);

        var request = Assert.Single(result.BuildResult.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterCreate, request.RequestType);
        Assert.Equal("RoomName", request.Payload!["parameterName"]);
        Assert.Equal("Lobby", request.Payload["requestedValue"]);
    }

    [Fact]
    public void Parameter_workflow_generates_create_request_for_missing_manufacturer()
    {
        var result = WriteLayerWorkflowSupport.RunParameterWorkflow(
            MissingManufacturerFixture.CreateBuildRequest(),
            _adapter);

        var request = Assert.Single(result.BuildResult.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterCreate, request.RequestType);
        Assert.Equal("Manufacturer", request.Payload!["parameterName"]);
    }

    [Fact]
    public void Parameter_workflow_generates_create_request_for_missing_acoustic_rating()
    {
        var result = WriteLayerWorkflowSupport.RunParameterWorkflow(
            MissingAcousticRatingFixture.CreateBuildRequest(),
            _adapter);

        var request = Assert.Single(result.BuildResult.WriteRequests!);
        Assert.Equal(WriteRequestType.ParameterCreate, request.RequestType);
        Assert.Equal("AcousticRating", request.Payload!["parameterName"]);
        Assert.Equal("45 dB", request.Payload["requestedValue"]);
    }

    [Fact]
    public void Naming_workflow_generates_rename_family_request_for_invalid_door_name()
    {
        var result = WriteLayerWorkflowSupport.RunNamingWorkflow(
            InvalidDoorNameFixture.CreateBuildRequest(),
            _adapter);

        var request = Assert.Single(result.BuildResult.WriteRequests!);
        Assert.Equal(WriteRequestType.RenameFamily, request.RequestType);
        Assert.Equal("Door_01", request.Payload!["currentName"]);
        Assert.Equal("DR_Door_01", request.Payload["proposedName"]);
        Assert.Equal("DR_{Token}", request.Payload["namingRule"]);
    }

    [Fact]
    public void Naming_workflow_generates_rename_family_request_for_invalid_window_name()
    {
        var result = WriteLayerWorkflowSupport.RunNamingWorkflow(
            InvalidWindowNameFixture.CreateBuildRequest(),
            _adapter);

        var request = Assert.Single(result.BuildResult.WriteRequests!);
        Assert.Equal(WriteRequestType.RenameFamily, request.RequestType);
        Assert.Equal("Window_01", request.Payload!["currentName"]);
        Assert.Equal("WN_Window_01", request.Payload["proposedName"]);
        Assert.Equal("WN_{Token}", request.Payload["namingRule"]);
    }

    [Fact]
    public void Parameter_workflow_constructs_transaction_and_propagates_correlation()
    {
        var result = WriteLayerWorkflowSupport.RunParameterWorkflow(
            MissingFireRatingFixture.CreateBuildRequest(),
            _adapter);

        Assert.Equal(CorrelationId, result.Transaction.CorrelationId);
        Assert.Equal(RuleId, result.Transaction.RuleId);
        Assert.Single(result.Transaction.WriteRequests);
        Assert.Equal(CorrelationId, result.WriteBatch.CorrelationId);
        Assert.Equal(CorrelationId, result.WriteLayerResult.ExecutionMetadata!.CorrelationId);
        Assert.Equal(CorrelationId, result.TransactionLayerResult.ExecutionMetadata!.CorrelationId);
        Assert.Equal("write-batch-parameter-001", result.WriteBatch.BatchId);
    }

    [Fact]
    public void Naming_workflow_constructs_transaction_and_propagates_metadata()
    {
        var result = WriteLayerWorkflowSupport.RunNamingWorkflow(
            InvalidWindowNameFixture.CreateBuildRequest(),
            _adapter);

        Assert.Equal("transaction-naming-fixes-001", result.Transaction.TransactionId);
        Assert.Equal("fixture-invalid-window-name", result.BuildResult.Metadata!["targetSetId"]);
        Assert.Equal(RuleId, result.BuildResult.Metadata["ruleId"]);
        Assert.Contains(result.BuildResult.Diagnostics!, diagnostic => diagnostic.Code == "NamingWriteRequestBuilder.Completed");
        Assert.NotEmpty(_adapter.Diagnostics.GetWriteDiagnostics());
    }

    [Fact]
    public void Parameter_workflow_generates_builder_statistics()
    {
        var result = WriteLayerWorkflowSupport.RunParameterWorkflow(
            MissingFireRatingFixture.CreateBuildRequest(),
            _adapter);

        Assert.Equal(1, result.BuildResult.Statistics!.FindingsProcessed);
        Assert.Equal(1, result.BuildResult.Statistics.RequestsGenerated);
        Assert.Equal(1, result.BuildResult.Statistics.CreateRequests);
        Assert.Equal(0, result.BuildResult.Statistics.UpdateRequests);
    }

    [Fact]
    public void Mixed_issues_parameter_and_naming_workflows_operate_together()
    {
        var parameterResult = WriteLayerWorkflowSupport.RunParameterWorkflow(
            MixedIssuesFixture.CreateParameterBuildRequest(),
            new RevitWriteAdapter());
        var namingResult = WriteLayerWorkflowSupport.RunNamingWorkflow(
            MixedIssuesFixture.CreateNamingBuildRequest(),
            new RevitWriteAdapter());

        Assert.Equal(WriteRequestType.ParameterCreate, parameterResult.BuildResult.WriteRequests!.Single().RequestType);
        Assert.Equal("RoomName", parameterResult.BuildResult.WriteRequests!.Single().Payload!["parameterName"]);
        Assert.Equal(WriteRequestType.RenameFamily, namingResult.BuildResult.WriteRequests!.Single().RequestType);
        Assert.Equal("DR_DoorMixed", namingResult.BuildResult.WriteRequests!.Single().Payload!["proposedName"]);
        Assert.Equal(CorrelationId, parameterResult.Transaction.CorrelationId);
        Assert.Equal(CorrelationId, namingResult.Transaction.CorrelationId);
    }

    [Fact]
    public void Large_correction_dataset_parameter_workflow_scales_deterministically()
    {
        var result = WriteLayerWorkflowSupport.RunParameterWorkflow(
            LargeCorrectionFixture.CreateParameterBuildRequest(),
            _adapter);

        Assert.Equal(LargeCorrectionFixture.ParameterIssueCount, result.BuildResult.WriteRequests!.Count);
        Assert.Equal(LargeCorrectionFixture.ParameterIssueCount, result.BuildResult.Statistics!.CreateRequests);
        Assert.Equal(LargeCorrectionFixture.ParameterIssueCount, result.Transaction.WriteRequests.Count);
        Assert.Equal(LargeCorrectionFixture.ParameterIssueCount, result.WriteLayerResult.RequestReferences!.Count);
    }

    [Fact]
    public void Large_correction_dataset_naming_workflow_scales_deterministically()
    {
        var result = WriteLayerWorkflowSupport.RunNamingWorkflow(
            LargeCorrectionFixture.CreateNamingBuildRequest(),
            _adapter);

        Assert.Equal(LargeCorrectionFixture.NamingIssueCount, result.BuildResult.WriteRequests!.Count);
        Assert.Equal(LargeCorrectionFixture.NamingIssueCount, result.BuildResult.Statistics!.RenameFamilyRequests);
        Assert.All(result.BuildResult.WriteRequests!, request =>
            Assert.StartsWith("WN_", request.Payload!["proposedName"], StringComparison.Ordinal));
    }

    [Fact]
    public void Write_preparation_workflow_produces_deterministic_requests()
    {
        var buildRequest = MissingFireRatingFixture.CreateBuildRequest();

        var first = WriteLayerWorkflowSupport.RunParameterWorkflow(buildRequest, new RevitWriteAdapter());
        var second = WriteLayerWorkflowSupport.RunParameterWorkflow(buildRequest, new RevitWriteAdapter());

        Assert.Equal(
            SerializeRequests(first.BuildResult.WriteRequests!),
            SerializeRequests(second.BuildResult.WriteRequests!));
        Assert.Equal(
            first.Transaction.TransactionId,
            second.Transaction.TransactionId);
    }

    [Fact]
    public void End_to_end_correction_preparation_collects_write_layer_results_without_execution()
    {
        var result = WriteLayerWorkflowSupport.RunParameterWorkflow(
            MissingFireRatingFixture.CreateBuildRequest(),
            _adapter);

        Assert.Equal(WriteRequestStatus.NotExecuted, result.WriteLayerResult.Status);
        Assert.Equal(TransactionStatus.Pending, result.TransactionLayerResult.Status);
        Assert.Contains(result.WriteLayerResult.Diagnostics!, diagnostic =>
            diagnostic.Code == "WriteRequestExecutor.NotImplemented");
        Assert.Single(_adapter.Results.GetWriteResults());
        Assert.Single(_adapter.Results.GetTransactionResults());
    }

    private static string SerializeRequests(IReadOnlyList<WriteRequest> requests)
    {
        return JsonSerializer.Serialize(requests.Select(request => new
        {
            request.RequestId,
            request.RequestType,
            request.TargetObject.Id,
            request.Payload,
            request.CorrelationId,
            request.RuleId
        }));
    }

    private const string CorrelationId = WriteLayerFixtureBuilder.CorrelationId;
    private const string RuleId = WriteLayerFixtureBuilder.RuleId;
}

public class WriteLayerArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyNames =
    [
        "BIMCapabilities.Runtime",
        "BIMCapabilities.Engines.Report",
        "BIMCapabilities.Launchers.Revit"
    ];

    [Fact]
    public void Write_adapter_assembly_references_only_contracts()
    {
        var referencedProjectAssemblies = typeof(RevitWriteAdapter).Assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name!.StartsWith("BIMCapabilities.", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal(["BIMCapabilities.Contracts"], referencedProjectAssemblies);
    }

    [Fact]
    public void Write_adapter_does_not_reference_revit_api_runtime_report_or_launcher()
    {
        var referencedAssemblies = typeof(RevitWriteAdapter).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }

        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Write_layer_skeleton_does_not_contain_execution_or_rollback_logic()
    {
        var writeTypes = typeof(RevitWriteAdapter).Assembly.GetTypes()
            .Where(type => type.Namespace == "BIMCapabilities.Adapters.Revit.Write")
            .Where(type => type.IsClass && !type.IsAbstract);

        Assert.All(writeTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Rollback", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Commit", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Write_layer_e2e_test_project_does_not_reference_runtime_report_or_launcher()
    {
        var referencedAssemblies = typeof(WriteLayerEndToEndTests).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }

        Assert.Contains("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Contracts", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Engines.Parameter", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Engines.Naming", referencedAssemblies);
    }
}
