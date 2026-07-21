using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class RevitWriteIntegrationFixtures
{
    internal const string CorrelationId = "corr-revit-write-integration-001";
    internal static readonly DateTimeOffset RequestedAt = new(2026, 6, 20, 16, 0, 0, TimeSpan.Zero);

    internal static WriteRequest CreateParameterCreateRequest()
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
            CorrelationId = CorrelationId,
            RuleId = "STD-ARC-OPENINGS-V01",
            RequestedAt = RequestedAt
        };
    }

    internal static WriteRequest CreateParameterUpdateRequest()
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
            CorrelationId = CorrelationId,
            RuleId = "STD-ARC-OPENINGS-V01",
            RequestedAt = RequestedAt
        };
    }

    internal static WriteRequest CreateRenameFamilyRequest()
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
            CorrelationId = CorrelationId,
            RuleId = "STD-ARC-OPENINGS-V01",
            RequestedAt = RequestedAt
        };
    }

    internal static WriteRequestBatch CreateWriteBatchRequest()
    {
        return new WriteRequestBatch
        {
            BatchId = "write-batch-001",
            Requests =
            [
                CreateParameterUpdateRequest(),
                CreateRenameFamilyRequest()
            ],
            Metadata = new Dictionary<string, string>
            {
                ["executionMode"] = "fix",
                ["sourceEngine"] = "parameter.compliance"
            },
            CorrelationId = CorrelationId,
            CreatedAt = RequestedAt
        };
    }

    internal static TransactionRequest CreateTransactionRequest()
    {
        return new TransactionRequest
        {
            TransactionId = "transaction-opening-fixes-001",
            Name = "Apply Opening Fixes",
            Description = "Applies parameter and naming fixes for opening families.",
            WriteRequests =
            [
                CreateParameterUpdateRequest(),
                CreateRenameFamilyRequest()
            ],
            Scope = new TransactionScope
            {
                Kind = TransactionScopeKind.MultipleObjects,
                TargetObjects =
                [
                    CreateFamilyTarget("family-001"),
                    CreateFamilyTarget("family-002")
                ]
            },
            Metadata = new Dictionary<string, string>
            {
                ["executionMode"] = "fix"
            },
            CorrelationId = CorrelationId,
            RuleId = "STD-ARC-OPENINGS-V01",
            Order = 1,
            RequestedAt = RequestedAt
        };
    }

    internal static TransactionBatch CreateTransactionBatch()
    {
        return new TransactionBatch
        {
            BatchId = "transaction-batch-001",
            Transactions = [CreateTransactionRequest()],
            CorrelationId = CorrelationId,
            CreatedAt = RequestedAt
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
}
