using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Engines.Parameter.Write;
using BIMCapabilities.Engines.Naming.Write;
using BIMCapabilities.Engines.Parameter.Write;

namespace BIMCapabilities.Adapters.Revit.Tests;

internal static class WriteLayerWorkflowSupport
{
    internal sealed record ParameterWorkflowResult(
        ParameterWriteRequestBuildResult BuildResult,
        WriteRequestBatch WriteBatch,
        TransactionRequest Transaction,
        WriteRequestResult WriteLayerResult,
        TransactionResult TransactionLayerResult);

    internal sealed record NamingWorkflowResult(
        NamingWriteRequestBuildResult BuildResult,
        WriteRequestBatch WriteBatch,
        TransactionRequest Transaction,
        WriteRequestResult WriteLayerResult,
        TransactionResult TransactionLayerResult);

    internal static ParameterWorkflowResult RunParameterWorkflow(
        ParameterWriteRequestBuildRequest buildRequest,
        RevitWriteAdapter adapter,
        string transactionId = "transaction-parameter-fixes-001",
        string transactionName = "Apply Parameter Fixes")
    {
        var builder = new ParameterWriteRequestBuilder();
        var buildResult = builder.Build(buildRequest);
        var writeRequests = buildResult.WriteRequests ?? [];
        var writeBatch = WriteLayerFixtureBuilder.CreateWriteRequestBatch(writeRequests, "write-batch-parameter-001");
        var transaction = WriteLayerFixtureBuilder.CreateTransaction(writeRequests, transactionId, transactionName);

        adapter.Diagnostics.Clear();
        adapter.Results.Clear();

        foreach (var diagnostic in buildResult.Diagnostics ?? [])
        {
            adapter.Diagnostics.Record(new WriteRequestDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = MapSeverity(diagnostic.Severity),
                Location = "builder:parameter.write-request-builder",
                Data = diagnostic.Data
            });
        }

        var writeLayerResult = adapter.WriteRequests.ExecuteBatch(writeBatch);
        adapter.Results.Collect(writeLayerResult);

        var transactionLayerResult = adapter.Transactions.Execute(transaction);
        adapter.Results.Collect(transactionLayerResult);

        return new ParameterWorkflowResult(
            buildResult,
            writeBatch,
            transaction,
            writeLayerResult,
            transactionLayerResult);
    }

    internal static NamingWorkflowResult RunNamingWorkflow(
        NamingWriteRequestBuildRequest buildRequest,
        RevitWriteAdapter adapter,
        string transactionId = "transaction-naming-fixes-001",
        string transactionName = "Apply Naming Fixes")
    {
        var builder = new NamingWriteRequestBuilder();
        var buildResult = builder.Build(buildRequest);
        var writeRequests = buildResult.WriteRequests ?? [];
        var writeBatch = WriteLayerFixtureBuilder.CreateWriteRequestBatch(writeRequests, "write-batch-naming-001");
        var transaction = WriteLayerFixtureBuilder.CreateTransaction(writeRequests, transactionId, transactionName);

        adapter.Diagnostics.Clear();
        adapter.Results.Clear();

        foreach (var diagnostic in buildResult.Diagnostics ?? [])
        {
            adapter.Diagnostics.Record(new WriteRequestDiagnostic
            {
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Severity = MapSeverity(diagnostic.Severity),
                Location = "builder:naming.write-request-builder",
                Data = diagnostic.Data
            });
        }

        var writeLayerResult = adapter.WriteRequests.ExecuteBatch(writeBatch);
        adapter.Results.Collect(writeLayerResult);

        var transactionLayerResult = adapter.Transactions.Execute(transaction);
        adapter.Results.Collect(transactionLayerResult);

        return new NamingWorkflowResult(
            buildResult,
            writeBatch,
            transaction,
            writeLayerResult,
            transactionLayerResult);
    }

    private static WriteRequestDiagnosticSeverity MapSeverity(ParameterWriteRequestBuildDiagnosticSeverity severity)
    {
        return severity switch
        {
            ParameterWriteRequestBuildDiagnosticSeverity.Warning => WriteRequestDiagnosticSeverity.Warning,
            ParameterWriteRequestBuildDiagnosticSeverity.Error => WriteRequestDiagnosticSeverity.Error,
            _ => WriteRequestDiagnosticSeverity.Information
        };
    }

    private static WriteRequestDiagnosticSeverity MapSeverity(NamingWriteRequestBuildDiagnosticSeverity severity)
    {
        return severity switch
        {
            NamingWriteRequestBuildDiagnosticSeverity.Warning => WriteRequestDiagnosticSeverity.Warning,
            NamingWriteRequestBuildDiagnosticSeverity.Error => WriteRequestDiagnosticSeverity.Error,
            _ => WriteRequestDiagnosticSeverity.Information
        };
    }
}
