using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Write;

/// <summary>
/// Deterministic stub responses used by the Revit Adapter write skeleton.
/// </summary>
internal static class RevitWriteStubResponses
{
    internal const string StubAdapterId = "revit-adapter-write-skeleton";
    internal const string StubNotImplementedMessage = "Revit API write execution is not implemented. Contract composition stub response returned.";

    internal static readonly DateTimeOffset StubExecutedAt = new(2026, 6, 20, 16, 0, 0, TimeSpan.Zero);

    internal static WriteRequestResult CreateWriteRequestResult(WriteRequest request)
    {
        return new WriteRequestResult
        {
            Status = WriteRequestStatus.NotExecuted,
            Diagnostics =
            [
                new WriteRequestDiagnostic
                {
                    Code = "WriteRequestExecutor.NotImplemented",
                    Message = StubNotImplementedMessage,
                    Severity = WriteRequestDiagnosticSeverity.Information,
                    RequestId = request.RequestId,
                    Location = "executor:write-request"
                }
            ],
            RequestReferences =
            [
                new WriteRequestReference
                {
                    RequestId = request.RequestId,
                    RequestType = request.RequestType,
                    Status = WriteRequestStatus.NotExecuted,
                    Order = request.Order
                }
            ],
            ExecutionMetadata = CreateWriteExecutionMetadata(request.CorrelationId, request.RequestId)
        };
    }

    internal static WriteRequestResult CreateWriteRequestBatchResult(WriteRequestBatch batch)
    {
        return new WriteRequestResult
        {
            Status = WriteRequestStatus.NotExecuted,
            Diagnostics =
            [
                new WriteRequestDiagnostic
                {
                    Code = "WriteRequestExecutor.NotImplemented",
                    Message = StubNotImplementedMessage,
                    Severity = WriteRequestDiagnosticSeverity.Information,
                    Location = "executor:write-request-batch"
                }
            ],
            RequestReferences = batch.Requests
                .Select(request => new WriteRequestReference
                {
                    RequestId = request.RequestId,
                    RequestType = request.RequestType,
                    Status = WriteRequestStatus.NotExecuted,
                    Order = request.Order
                })
                .ToArray(),
            ExecutionMetadata = CreateWriteExecutionMetadata(batch.CorrelationId, batchId: batch.BatchId)
        };
    }

    internal static TransactionResult CreateTransactionResult(TransactionRequest request)
    {
        return new TransactionResult
        {
            Status = TransactionStatus.Pending,
            ExecutedRequests = request.WriteRequests
                .Select(writeRequest => new WriteRequestReference
                {
                    RequestId = writeRequest.RequestId,
                    RequestType = writeRequest.RequestType,
                    Status = WriteRequestStatus.NotExecuted,
                    Order = writeRequest.Order
                })
                .ToArray(),
            Diagnostics =
            [
                new TransactionDiagnostic
                {
                    Code = "TransactionExecutor.NotImplemented",
                    Message = StubNotImplementedMessage,
                    Severity = TransactionDiagnosticSeverity.Information,
                    TransactionId = request.TransactionId,
                    Location = "executor:transaction"
                }
            ],
            ExecutionMetadata = CreateTransactionExecutionMetadata(
                request.CorrelationId,
                request.TransactionId)
        };
    }

    internal static TransactionResult CreateTransactionBatchResult(TransactionBatch batch)
    {
        var references = batch.Transactions
            .SelectMany(transaction => transaction.WriteRequests)
            .Select(writeRequest => new WriteRequestReference
            {
                RequestId = writeRequest.RequestId,
                RequestType = writeRequest.RequestType,
                Status = WriteRequestStatus.NotExecuted,
                Order = writeRequest.Order
            })
            .ToArray();

        return new TransactionResult
        {
            Status = TransactionStatus.Pending,
            ExecutedRequests = references,
            Diagnostics =
            [
                new TransactionDiagnostic
                {
                    Code = "TransactionExecutor.NotImplemented",
                    Message = StubNotImplementedMessage,
                    Severity = TransactionDiagnosticSeverity.Information,
                    Location = "executor:transaction-batch"
                }
            ],
            ExecutionMetadata = CreateTransactionExecutionMetadata(
                batch.CorrelationId,
                batchId: batch.BatchId)
        };
    }

    internal static WriteRequest CreateParameterCreateRequest(string correlationId)
    {
        return new WriteRequest
        {
            RequestId = "write-request-parameter-create-001",
            TargetObject = CreateFamilyTarget("family-001"),
            RequestType = WriteRequestType.ParameterCreate,
            Order = 1,
            Payload = new Dictionary<string, string>
            {
                ["parameterName"] = "FireRating",
                ["parameterType"] = "Text",
                ["value"] = "60 min"
            },
            CorrelationId = correlationId,
            RuleId = "STD-ARC-OPENINGS-V01",
            RequestedAt = StubExecutedAt
        };
    }

    internal static WriteRequest CreateParameterUpdateRequest(string correlationId)
    {
        return new WriteRequest
        {
            RequestId = "write-request-parameter-update-001",
            TargetObject = CreateFamilyTarget("family-001"),
            RequestType = WriteRequestType.ParameterUpdate,
            Order = 1,
            Payload = new Dictionary<string, string>
            {
                ["parameterName"] = "FireRating",
                ["value"] = "60 min"
            },
            CorrelationId = correlationId,
            RuleId = "STD-ARC-OPENINGS-V01",
            RequestedAt = StubExecutedAt
        };
    }

    internal static WriteRequest CreateRenameFamilyRequest(string correlationId)
    {
        return new WriteRequest
        {
            RequestId = "write-request-rename-family-001",
            TargetObject = CreateFamilyTarget("family-002"),
            RequestType = WriteRequestType.RenameFamily,
            Order = 1,
            Payload = new Dictionary<string, string>
            {
                ["newName"] = "DR_SingleDoor"
            },
            CorrelationId = correlationId,
            RuleId = "STD-ARC-OPENINGS-V01",
            RequestedAt = StubExecutedAt
        };
    }

    private static NormalizedIdentifier CreateFamilyTarget(string id)
    {
        return new NormalizedIdentifier
        {
            Id = id,
            Kind = "family",
            Scope = "project-document"
        };
    }

    private static WriteRequestExecutionMetadata CreateWriteExecutionMetadata(
        string? correlationId,
        string? requestId = null,
        string? batchId = null)
    {
        return new WriteRequestExecutionMetadata
        {
            ExecutedAt = StubExecutedAt,
            CorrelationId = correlationId,
            BatchId = batchId,
            AdapterId = StubAdapterId,
            Properties = new Dictionary<string, string>
            {
                ["implementation"] = "skeleton",
                ["requestId"] = requestId ?? string.Empty
            }
        };
    }

    private static TransactionExecutionMetadata CreateTransactionExecutionMetadata(
        string? correlationId,
        string? transactionId = null,
        string? batchId = null)
    {
        return new TransactionExecutionMetadata
        {
            ExecutedAt = StubExecutedAt,
            CorrelationId = correlationId,
            BatchId = batchId,
            TransactionId = transactionId,
            AdapterId = StubAdapterId,
            Properties = new Dictionary<string, string>
            {
                ["implementation"] = "skeleton"
            }
        };
    }
}
