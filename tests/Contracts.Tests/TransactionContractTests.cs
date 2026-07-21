using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Contracts.Tests;

public class TransactionContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = TransactionSerialization.Options;

    [Fact]
    public void Transaction_contracts_are_data_only_types()
    {
        var transactionTypes = typeof(TransactionRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(TransactionRequest).Namespace)
            .Where(type => type.Name.StartsWith("Transaction", StringComparison.Ordinal))
            .Where(type => type != typeof(TransactionSerialization))
            .Where(type => type != typeof(ITransactionRequest))
            .Where(type => !type.IsEnum)
            .Where(type => !type.Name.EndsWith("Severity", StringComparison.Ordinal));

        Assert.All(transactionTypes, type =>
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void TransactionRequest_implements_ITransactionRequest()
    {
        Assert.True(typeof(ITransactionRequest).IsAssignableFrom(typeof(TransactionRequest)));
    }

    [Fact]
    public void TransactionRequest_can_be_constructed_with_required_properties()
    {
        var transaction = TransactionContractTestData.CreateParameterFixTransaction();

        Assert.Equal("transaction-parameter-fix-001", transaction.TransactionId);
        Assert.Equal("Apply Opening Parameter Fixes", transaction.Name);
        Assert.Contains("opening parameters", transaction.Description, StringComparison.Ordinal);
        Assert.Single(transaction.WriteRequests);
        Assert.Equal(TransactionScopeKind.MultipleObjects, transaction.Scope!.Kind);
        Assert.Equal(TransactionContractTestData.RuleId, transaction.RuleId);
        Assert.Equal(TransactionContractTestData.CorrelationId, transaction.CorrelationId);
        Assert.Equal(TransactionContractTestData.RequestedAt, transaction.RequestedAt);
    }

    [Fact]
    public void TransactionBatch_supports_multiple_ordered_transactions()
    {
        var batch = TransactionContractTestData.CreateBatch();

        Assert.Equal("transaction-batch-001", batch.BatchId);
        Assert.Equal(2, batch.Transactions.Count);
        Assert.Equal(1, batch.Transactions[0].Order);
        Assert.Equal(2, batch.Transactions[1].Order);
        Assert.Equal("sequential", batch.Metadata!["executionPolicy"]);
        Assert.Equal(TransactionContractTestData.CorrelationId, batch.CorrelationId);
    }

    [Theory]
    [InlineData(TransactionStatus.Pending)]
    [InlineData(TransactionStatus.Running)]
    [InlineData(TransactionStatus.Completed)]
    [InlineData(TransactionStatus.Failed)]
    [InlineData(TransactionStatus.RolledBack)]
    [InlineData(TransactionStatus.Cancelled)]
    public void TransactionStatus_supports_required_status_values(TransactionStatus status)
    {
        var result = TransactionContractTestData.CreateCompletedResult() with
        {
            Status = status
        };

        Assert.Equal(status, result.Status);
    }

    [Theory]
    [InlineData(TransactionScopeKind.SingleObject)]
    [InlineData(TransactionScopeKind.MultipleObjects)]
    [InlineData(TransactionScopeKind.ModelScope)]
    [InlineData(TransactionScopeKind.Custom)]
    public void TransactionScope_supports_required_scope_values(TransactionScopeKind scopeKind)
    {
        var scope = scopeKind switch
        {
            TransactionScopeKind.SingleObject => TransactionContractTestData.CreateSingleObjectScope(),
            TransactionScopeKind.MultipleObjects => TransactionContractTestData.CreateMultipleObjectScope(),
            TransactionScopeKind.ModelScope => TransactionContractTestData.CreateModelScope(),
            _ => TransactionContractTestData.CreateCustomScope()
        };

        Assert.Equal(scopeKind, scope.Kind);
    }

    [Fact]
    public void TransactionResult_supports_status_executed_requests_diagnostics_and_execution_metadata()
    {
        var result = TransactionContractTestData.CreateCompletedResult();

        Assert.Equal(TransactionStatus.Completed, result.Status);
        Assert.Equal(2, result.ExecutedRequests!.Count);
        Assert.Single(result.Diagnostics!);
        Assert.Equal("transaction-parameter-fix-001", result.ExecutionMetadata!.TransactionId);
        Assert.Equal("revit.adapter.write", result.ExecutionMetadata.AdapterId);
    }

    [Fact]
    public void TransactionDiagnostic_supports_required_structure()
    {
        var diagnostic = TransactionContractTestData.CreateRolledBackResult().Diagnostics!.Single();

        Assert.Equal("Transaction.RolledBack", diagnostic.Code);
        Assert.Contains("rolled back", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TransactionDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("transaction-parameter-fix-001", diagnostic.TransactionId);
        Assert.Equal("write-request-parameter-update-001", diagnostic.Data!["failedRequestId"]);
    }

    [Fact]
    public void TransactionRequest_supports_json_round_trip_serialization()
    {
        var original = TransactionContractTestData.CreateParameterFixTransaction();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<TransactionRequest>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.TransactionId, roundTrip.TransactionId);
        Assert.Equal(original.Name, roundTrip.Name);
        Assert.Equal(original.WriteRequests.Count, roundTrip.WriteRequests.Count);
        Assert.Equal(original.Scope!.Kind, roundTrip.Scope!.Kind);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void TransactionBatch_supports_json_round_trip_serialization()
    {
        var original = TransactionContractTestData.CreateBatch();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<TransactionBatch>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.BatchId, roundTrip.BatchId);
        Assert.Equal(original.Transactions.Count, roundTrip.Transactions.Count);
        Assert.Equal(original.Transactions[1].Name, roundTrip.Transactions[1].Name);
    }

    [Fact]
    public void TransactionResult_supports_json_round_trip_serialization()
    {
        var original = TransactionContractTestData.CreateCompletedResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<TransactionResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Status, roundTrip.Status);
        Assert.Equal(original.ExecutedRequests!.Count, roundTrip.ExecutedRequests!.Count);
        Assert.Equal(original.Diagnostics![0].Code, roundTrip.Diagnostics![0].Code);
        Assert.Equal(original.ExecutionMetadata!.BatchId, roundTrip.ExecutionMetadata!.BatchId);
    }

    [Fact]
    public void Transaction_contracts_do_not_reference_revit_assemblies()
    {
        var assembly = typeof(TransactionRequest).Assembly;
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Transaction_namespace_does_not_define_execution_or_rollback_types()
    {
        var transactionTypes = typeof(TransactionRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(TransactionRequest).Namespace)
            .Where(type => type.Name.StartsWith("Transaction", StringComparison.Ordinal));

        Assert.All(transactionTypes, type =>
        {
            Assert.DoesNotContain("Executor", type.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Processor", type.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RollbackService", type.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TransactionManager", type.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void TransactionRequest_groups_write_requests_into_execution_unit()
    {
        var transaction = TransactionContractTestData.CreateNamingFixTransaction();

        Assert.Equal(2, transaction.WriteRequests.Count);
        Assert.Equal(WriteRequestType.RenameFamily, transaction.WriteRequests[0].RequestType);
        Assert.Equal(WriteRequestType.RenameType, transaction.WriteRequests[1].RequestType);
    }
}
