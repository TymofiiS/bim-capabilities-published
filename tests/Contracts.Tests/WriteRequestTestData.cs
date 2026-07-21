using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Contracts.Tests;

internal static class WriteRequestTestData
{
    internal static readonly DateTimeOffset RequestedAt = new(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 14, 0, 5, TimeSpan.Zero);

    internal const string RuleId = "STD-ARC-OPENINGS-V01";
    internal const string CorrelationId = "corr-write-request-001";

    internal static WriteRequest CreateParameterUpdateRequest()
    {
        return new WriteRequest
        {
            RequestId = "write-request-parameter-update-001",
            TargetObject = CreateFamilyTarget("family-001", "DR_SingleDoor"),
            RequestType = WriteRequestType.ParameterUpdate,
            Order = 1,
            Payload = new Dictionary<string, string>
            {
                ["parameterName"] = "FireRating",
                ["value"] = "60 min"
            },
            Metadata = new Dictionary<string, string>
            {
                ["engineId"] = "parameter.compliance",
                ["operation"] = "parameter-update"
            },
            CorrelationId = CorrelationId,
            RuleId = RuleId,
            RequestedAt = RequestedAt
        };
    }

    internal static WriteRequest CreateRenameFamilyRequest()
    {
        return new WriteRequest
        {
            RequestId = "write-request-rename-family-001",
            TargetObject = CreateFamilyTarget("family-002", "Door_Single"),
            RequestType = WriteRequestType.RenameFamily,
            Order = 2,
            Payload = new Dictionary<string, string>
            {
                ["newName"] = "DR_SingleDoor"
            },
            Metadata = new Dictionary<string, string>
            {
                ["engineId"] = "naming.compliance",
                ["operation"] = "rename-family"
            },
            CorrelationId = CorrelationId,
            RuleId = RuleId,
            RequestedAt = RequestedAt
        };
    }

    internal static WriteRequest CreateRenameTypeRequest()
    {
        return new WriteRequest
        {
            RequestId = "write-request-rename-type-001",
            TargetObject = CreateFamilyTypeTarget("family-type-001", "DR_SingleDoor900x2100"),
            RequestType = WriteRequestType.RenameType,
            Order = 3,
            Payload = new Dictionary<string, string>
            {
                ["newName"] = "DR_SingleDoor900x2100"
            },
            CorrelationId = CorrelationId,
            RuleId = RuleId,
            RequestedAt = RequestedAt
        };
    }

    internal static WriteRequestBatch CreateBatch()
    {
        return new WriteRequestBatch
        {
            BatchId = "write-batch-001",
            Requests =
            [
                CreateParameterUpdateRequest(),
                CreateRenameFamilyRequest(),
                CreateRenameTypeRequest()
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

    internal static WriteRequestResult CreateSucceededResult()
    {
        return new WriteRequestResult
        {
            Status = WriteRequestStatus.Succeeded,
            Diagnostics =
            [
                new WriteRequestDiagnostic
                {
                    Code = "WriteRequest.Completed",
                    Message = "All write requests completed successfully.",
                    Severity = WriteRequestDiagnosticSeverity.Information,
                    Location = "adapter:revit-write"
                }
            ],
            RequestReferences =
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
            ExecutionMetadata = new WriteRequestExecutionMetadata
            {
                ExecutedAt = ExecutedAt,
                CorrelationId = CorrelationId,
                BatchId = "write-batch-001",
                AdapterId = "revit.adapter.write",
                Properties = new Dictionary<string, string>
                {
                    ["requestCount"] = "2"
                }
            }
        };
    }

    internal static WriteRequestResult CreateFailedResult()
    {
        return new WriteRequestResult
        {
            Status = WriteRequestStatus.Failed,
            Diagnostics =
            [
                new WriteRequestDiagnostic
                {
                    Code = "WriteRequest.Failed",
                    Message = "Parameter update failed because the target object was not found.",
                    Severity = WriteRequestDiagnosticSeverity.Error,
                    RequestId = "write-request-parameter-update-001",
                    Location = "adapter:revit-write",
                    Data = new Dictionary<string, string>
                    {
                        ["parameterName"] = "FireRating"
                    }
                }
            ],
            RequestReferences =
            [
                new WriteRequestReference
                {
                    RequestId = "write-request-parameter-update-001",
                    RequestType = WriteRequestType.ParameterUpdate,
                    Status = WriteRequestStatus.Failed,
                    Order = 1
                }
            ],
            ExecutionMetadata = new WriteRequestExecutionMetadata
            {
                ExecutedAt = ExecutedAt,
                CorrelationId = CorrelationId,
                BatchId = "write-batch-001",
                AdapterId = "revit.adapter.write"
            }
        };
    }

    private static NormalizedIdentifier CreateFamilyTarget(string id, string _)
    {
        return new NormalizedIdentifier
        {
            Id = id,
            Kind = "family",
            Scope = "project-document"
        };
    }

    private static NormalizedIdentifier CreateFamilyTypeTarget(string id, string _)
    {
        return new NormalizedIdentifier
        {
            Id = id,
            Kind = "familyType",
            Scope = "project-document"
        };
    }
}
