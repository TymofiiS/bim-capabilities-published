using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Contracts.Tests;

internal static class TransactionContractTestData
{
    internal static readonly DateTimeOffset RequestedAt = new(2026, 6, 20, 15, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 15, 0, 10, TimeSpan.Zero);

    internal const string RuleId = "STD-ARC-OPENINGS-V01";
    internal const string CorrelationId = "corr-transaction-001";

    internal static TransactionRequest CreateParameterFixTransaction()
    {
        return new TransactionRequest
        {
            TransactionId = "transaction-parameter-fix-001",
            Name = "Apply Opening Parameter Fixes",
            Description = "Updates required opening parameters for selected families.",
            WriteRequests =
            [
                WriteRequestTestData.CreateParameterUpdateRequest()
            ],
            Scope = CreateMultipleObjectScope(),
            Metadata = new Dictionary<string, string>
            {
                ["executionMode"] = "fix",
                ["sourceEngine"] = "parameter.compliance"
            },
            CorrelationId = CorrelationId,
            RuleId = RuleId,
            Order = 1,
            RequestedAt = RequestedAt
        };
    }

    internal static TransactionRequest CreateNamingFixTransaction()
    {
        return new TransactionRequest
        {
            TransactionId = "transaction-naming-fix-001",
            Name = "Apply Opening Naming Fixes",
            Description = "Renames door and window families to required naming standards.",
            WriteRequests =
            [
                WriteRequestTestData.CreateRenameFamilyRequest(),
                WriteRequestTestData.CreateRenameTypeRequest()
            ],
            Scope = CreateSingleObjectScope(),
            CorrelationId = CorrelationId,
            RuleId = RuleId,
            Order = 2,
            RequestedAt = RequestedAt
        };
    }

    internal static TransactionBatch CreateBatch()
    {
        return new TransactionBatch
        {
            BatchId = "transaction-batch-001",
            Transactions =
            [
                CreateParameterFixTransaction(),
                CreateNamingFixTransaction()
            ],
            Metadata = new Dictionary<string, string>
            {
                ["executionPolicy"] = "sequential",
                ["ruleId"] = RuleId
            },
            CorrelationId = CorrelationId,
            CreatedAt = RequestedAt
        };
    }

    internal static TransactionResult CreateCompletedResult()
    {
        return new TransactionResult
        {
            Status = TransactionStatus.Completed,
            ExecutedRequests =
            [
                new WriteRequestReference
                {
                    RequestId = "write-request-parameter-update-001",
                    RequestType = WriteRequestType.ParameterUpdate,
                    Status = WriteRequestStatus.Succeeded,
                    Order = 1
                },
                new WriteRequestReference
                {
                    RequestId = "write-request-rename-family-001",
                    RequestType = WriteRequestType.RenameFamily,
                    Status = WriteRequestStatus.Succeeded,
                    Order = 2
                }
            ],
            Diagnostics =
            [
                new TransactionDiagnostic
                {
                    Code = "Transaction.Completed",
                    Message = "Transaction completed successfully.",
                    Severity = TransactionDiagnosticSeverity.Information,
                    TransactionId = "transaction-parameter-fix-001",
                    Location = "adapter:revit-write"
                }
            ],
            ExecutionMetadata = new TransactionExecutionMetadata
            {
                ExecutedAt = ExecutedAt,
                CorrelationId = CorrelationId,
                BatchId = "transaction-batch-001",
                TransactionId = "transaction-parameter-fix-001",
                AdapterId = "revit.adapter.write",
                Properties = new Dictionary<string, string>
                {
                    ["executedRequestCount"] = "2"
                }
            }
        };
    }

    internal static TransactionResult CreateRolledBackResult()
    {
        return new TransactionResult
        {
            Status = TransactionStatus.RolledBack,
            ExecutedRequests =
            [
                new WriteRequestReference
                {
                    RequestId = "write-request-parameter-update-001",
                    RequestType = WriteRequestType.ParameterUpdate,
                    Status = WriteRequestStatus.Failed,
                    Order = 1
                }
            ],
            Diagnostics =
            [
                new TransactionDiagnostic
                {
                    Code = "Transaction.RolledBack",
                    Message = "Transaction was rolled back after a write request failed.",
                    Severity = TransactionDiagnosticSeverity.Error,
                    TransactionId = "transaction-parameter-fix-001",
                    Location = "adapter:revit-write",
                    Data = new Dictionary<string, string>
                    {
                        ["failedRequestId"] = "write-request-parameter-update-001"
                    }
                }
            ],
            ExecutionMetadata = new TransactionExecutionMetadata
            {
                ExecutedAt = ExecutedAt,
                CorrelationId = CorrelationId,
                BatchId = "transaction-batch-001",
                TransactionId = "transaction-parameter-fix-001",
                AdapterId = "revit.adapter.write"
            }
        };
    }

    internal static TransactionScope CreateSingleObjectScope()
    {
        return new TransactionScope
        {
            Kind = TransactionScopeKind.SingleObject,
            TargetObjects =
            [
                new NormalizedIdentifier
                {
                    Id = "family-002",
                    Kind = "family",
                    Scope = "project-document"
                }
            ]
        };
    }

    internal static TransactionScope CreateMultipleObjectScope()
    {
        return new TransactionScope
        {
            Kind = TransactionScopeKind.MultipleObjects,
            TargetObjects =
            [
                new NormalizedIdentifier { Id = "family-001", Kind = "family" },
                new NormalizedIdentifier { Id = "family-type-001", Kind = "familyType" }
            ]
        };
    }

    internal static TransactionScope CreateModelScope()
    {
        return new TransactionScope
        {
            Kind = TransactionScopeKind.ModelScope,
            Metadata = new Dictionary<string, string>
            {
                ["documentType"] = "project"
            }
        };
    }

    internal static TransactionScope CreateCustomScope()
    {
        return new TransactionScope
        {
            Kind = TransactionScopeKind.Custom,
            ScopeIdentifiers = ["custom-scope-openings-001"],
            Metadata = new Dictionary<string, string>
            {
                ["scopeName"] = "opening-families"
            }
        };
    }
}
