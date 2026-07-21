using System.Reflection;
using BIMCapabilities.Adapters.Revit.Tests.Fixtures;
using BIMCapabilities.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class RevitWriteIntegrationTests
{
    private readonly RevitWriteAdapter _adapter = new();

    [Fact]
    public void Revit_write_adapter_composes_all_required_services()
    {
        Assert.IsAssignableFrom<IRevitWriteAdapter>(_adapter);
        Assert.IsAssignableFrom<IWriteRequestExecutor>(_adapter.WriteRequests);
        Assert.IsAssignableFrom<ITransactionExecutor>(_adapter.Transactions);
        Assert.IsAssignableFrom<IWriteDiagnostics>(_adapter.Diagnostics);
        Assert.IsAssignableFrom<IWriteResultCollector>(_adapter.Results);
    }

    [Fact]
    public void Write_request_flow_returns_stub_result_without_execution()
    {
        var request = RevitWriteIntegrationFixtures.CreateParameterUpdateRequest();

        var result = _adapter.WriteRequests.Execute(request);

        Assert.Equal(WriteRequestStatus.NotExecuted, result.Status);
        Assert.Equal(CorrelationId, result.ExecutionMetadata!.CorrelationId);
        Assert.Equal("revit-adapter-write-skeleton", result.ExecutionMetadata.AdapterId);
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "WriteRequestExecutor.NotImplemented");
        Assert.Equal(request.RequestId, result.RequestReferences!.Single().RequestId);
    }

    [Fact]
    public void Parameter_create_request_flow_returns_stub_result()
    {
        var request = RevitWriteIntegrationFixtures.CreateParameterCreateRequest();

        var result = _adapter.WriteRequests.Execute(request);

        Assert.Equal(WriteRequestType.ParameterCreate, request.RequestType);
        Assert.Equal(WriteRequestStatus.NotExecuted, result.Status);
        Assert.Equal("FireRating", request.Payload!["parameterName"]);
    }

    [Fact]
    public void Rename_family_request_flow_returns_stub_result()
    {
        var request = RevitWriteIntegrationFixtures.CreateRenameFamilyRequest();

        var result = _adapter.WriteRequests.Execute(request);

        Assert.Equal(WriteRequestType.RenameFamily, request.RequestType);
        Assert.Equal("DR_SingleDoor", request.Payload!["newName"]);
        Assert.Equal(WriteRequestStatus.NotExecuted, result.Status);
    }

    [Fact]
    public void Write_batch_flow_returns_stub_result_for_all_requests()
    {
        var batch = RevitWriteIntegrationFixtures.CreateWriteBatchRequest();

        var result = _adapter.WriteRequests.ExecuteBatch(batch);

        Assert.Equal(WriteRequestStatus.NotExecuted, result.Status);
        Assert.Equal(2, result.RequestReferences!.Count);
        Assert.Equal(WriteRequestType.ParameterUpdate, result.RequestReferences[0].RequestType);
        Assert.Equal(WriteRequestType.RenameFamily, result.RequestReferences[1].RequestType);
    }

    [Fact]
    public void Transaction_flow_returns_stub_result_without_execution()
    {
        var transaction = RevitWriteIntegrationFixtures.CreateTransactionRequest();

        var result = _adapter.Transactions.Execute(transaction);

        Assert.Equal(TransactionStatus.Pending, result.Status);
        Assert.Equal(2, result.ExecutedRequests!.Count);
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "TransactionExecutor.NotImplemented");
        Assert.Equal(transaction.TransactionId, result.ExecutionMetadata!.TransactionId);
    }

    [Fact]
    public void Result_flow_collects_write_and_transaction_results()
    {
        var writeResult = _adapter.WriteRequests.Execute(RevitWriteIntegrationFixtures.CreateParameterUpdateRequest());
        var transactionResult = _adapter.Transactions.Execute(RevitWriteIntegrationFixtures.CreateTransactionRequest());

        _adapter.Results.Collect(writeResult);
        _adapter.Results.Collect(transactionResult);

        Assert.Single(_adapter.Results.GetWriteResults());
        Assert.Single(_adapter.Results.GetTransactionResults());
        Assert.Equal(WriteRequestStatus.NotExecuted, _adapter.Results.GetWriteResults()[0].Status);
        Assert.Equal(TransactionStatus.Pending, _adapter.Results.GetTransactionResults()[0].Status);
    }

    [Fact]
    public void Diagnostics_flow_records_write_and_transaction_diagnostics()
    {
        var writeResult = _adapter.WriteRequests.Execute(RevitWriteIntegrationFixtures.CreateParameterUpdateRequest());
        var transactionResult = _adapter.Transactions.Execute(RevitWriteIntegrationFixtures.CreateTransactionRequest());

        foreach (var diagnostic in writeResult.Diagnostics!)
        {
            _adapter.Diagnostics.Record(diagnostic);
        }

        foreach (var diagnostic in transactionResult.Diagnostics!)
        {
            _adapter.Diagnostics.Record(diagnostic);
        }

        Assert.Single(_adapter.Diagnostics.GetWriteDiagnostics());
        Assert.Single(_adapter.Diagnostics.GetTransactionDiagnostics());
    }

    [Fact]
    public void Composition_flow_supports_end_to_end_write_workflow()
    {
        _adapter.Diagnostics.Clear();
        _adapter.Results.Clear();

        var batch = RevitWriteIntegrationFixtures.CreateWriteBatchRequest();
        var transaction = RevitWriteIntegrationFixtures.CreateTransactionRequest();

        var writeResult = _adapter.WriteRequests.ExecuteBatch(batch);
        var transactionResult = _adapter.Transactions.Execute(transaction);

        foreach (var diagnostic in writeResult.Diagnostics!)
        {
            _adapter.Diagnostics.Record(diagnostic);
        }

        foreach (var diagnostic in transactionResult.Diagnostics!)
        {
            _adapter.Diagnostics.Record(diagnostic);
        }

        _adapter.Results.Collect(writeResult);
        _adapter.Results.Collect(transactionResult);

        Assert.Equal(2, writeResult.RequestReferences!.Count);
        Assert.Equal(2, transactionResult.ExecutedRequests!.Count);
        Assert.Single(_adapter.Results.GetWriteResults());
        Assert.Single(_adapter.Results.GetTransactionResults());
        Assert.Single(_adapter.Diagnostics.GetWriteDiagnostics());
        Assert.Single(_adapter.Diagnostics.GetTransactionDiagnostics());
    }

    [Fact]
    public void Transaction_batch_composition_flow_returns_stub_result()
    {
        var batch = RevitWriteIntegrationFixtures.CreateTransactionBatch();

        var result = _adapter.Transactions.ExecuteBatch(batch);

        Assert.Equal(TransactionStatus.Pending, result.Status);
        Assert.Equal(2, result.ExecutedRequests!.Count);
        Assert.Equal(CorrelationId, result.ExecutionMetadata!.CorrelationId);
    }

    [Fact]
    public void Write_composition_returns_deterministic_stub_responses()
    {
        var request = RevitWriteIntegrationFixtures.CreateParameterUpdateRequest();

        var first = _adapter.WriteRequests.Execute(request);
        var second = _adapter.WriteRequests.Execute(request);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.ExecutionMetadata!.ExecutedAt, second.ExecutionMetadata!.ExecutedAt);
        Assert.Equal(first.RequestReferences![0].RequestId, second.RequestReferences![0].RequestId);
    }

    private const string CorrelationId = RevitWriteIntegrationFixtures.CorrelationId;
}

public class RevitWriteArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyNames =
    [
        "BIMCapabilities.Runtime",
        "BIMCapabilities.Engines.Family",
        "BIMCapabilities.Engines.Parameter",
        "BIMCapabilities.Engines.Naming",
        "BIMCapabilities.Engines.Report",
        "BIMCapabilities.Launchers.Revit"
    ];

    private static readonly string[] TestProjectForbiddenAssemblyNames =
    [
        "BIMCapabilities.Runtime",
        "BIMCapabilities.Engines.Report",
        "BIMCapabilities.Launchers.Revit"
    ];

    [Fact]
    public void Write_layer_assembly_references_only_contracts()
    {
        var adapterAssembly = typeof(RevitWriteAdapter).Assembly;
        var referencedProjectAssemblies = adapterAssembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name!.StartsWith("BIMCapabilities.", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal(["BIMCapabilities.Contracts"], referencedProjectAssemblies);
    }

    [Fact]
    public void Write_layer_does_not_reference_forbidden_assemblies_or_revit_api()
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
    public void Write_skeleton_does_not_contain_execution_or_rollback_logic()
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
                Assert.DoesNotContain("Revit", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Autodesk", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Write_layer_test_project_does_not_reference_runtime_report_or_launcher()
    {
        var referencedAssemblies = typeof(RevitWriteIntegrationTests).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in TestProjectForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }

        Assert.Contains("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Contracts", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Engines.Parameter", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Engines.Naming", referencedAssemblies);
    }

    [Fact]
    public void Write_request_executor_returns_not_executed_status()
    {
        var result = new RevitWriteAdapter().WriteRequests.Execute(
            RevitWriteIntegrationFixtures.CreateParameterUpdateRequest());

        Assert.Equal(WriteRequestStatus.NotExecuted, result.Status);
        Assert.DoesNotContain(result.Diagnostics!, diagnostic =>
            diagnostic.Code.Contains("Succeeded", StringComparison.OrdinalIgnoreCase));
    }
}
