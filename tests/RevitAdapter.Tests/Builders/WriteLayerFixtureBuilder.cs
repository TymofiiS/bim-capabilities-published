using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Builders;

/// <summary>
/// Builds deterministic compliance findings and write-layer workflow inputs for E2E tests.
/// </summary>
internal static class WriteLayerFixtureBuilder
{
    internal const string RuleId = "STD-ARC-OPENINGS-V01";
    internal const string CorrelationId = "corr-write-layer-e2e-001";
    internal const string DoorPrefix = "DR_";
    internal const string WindowPrefix = "WN_";

    internal static readonly DateTimeOffset RequestedAt = new(2026, 6, 20, 19, 0, 0, TimeSpan.Zero);

    internal const string FireRatingGuid = "f1a2b3c4-d5e6-7890-abcd-ef1234567890";
    internal const string RoomNameGuid = "b2c3d4e5-f6a7-8901-bcde-f12345678902";
    internal const string AcousticRatingGuid = "a1b2c3d4-e5f6-7890-abcd-ef1234567891";
    internal const string ManufacturerGuid = "c3d4e5f6-a7b8-9012-cdef-123456789013";

    internal static NormalizedFamily CreateDoorFamily(string id, string name)
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier
            {
                Id = id,
                Kind = "family",
                Scope = "project-document"
            },
            Name = name,
            Category = new NormalizedCategory
            {
                Identifier = new NormalizedIdentifier { Id = "category-doors", Kind = "category" },
                Name = "Doors"
            }
        };
    }

    internal static NormalizedFamily CreateWindowFamily(string id, string name)
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier
            {
                Id = id,
                Kind = "family",
                Scope = "project-document"
            },
            Name = name,
            Category = new NormalizedCategory
            {
                Identifier = new NormalizedIdentifier { Id = "category-windows", Kind = "category" },
                Name = "Windows"
            }
        };
    }

    internal static ParameterTargetSet CreateParameterTargetSet(string targetSetId, params NormalizedFamily[] families)
    {
        return new ParameterTargetSet
        {
            TargetSetId = targetSetId,
            TargetFamilies = families
        };
    }

    internal static NamingTargetSet CreateNamingTargetSet(string targetSetId, params NormalizedFamily[] families)
    {
        return new NamingTargetSet
        {
            TargetSetId = targetSetId,
            TargetFamilies = families,
            SelectionMetadata = new Dictionary<string, string>
            {
                ["ruleId"] = RuleId,
                ["fixtureSource"] = "write-layer-e2e"
            }
        };
    }

    internal static ParameterComplianceResult CreateMissingParameterResult(
        string objectId,
        string objectName,
        string parameterName,
        string validationStage = "existence",
        string status = "Missing")
    {
        return new ParameterComplianceResult
        {
            EngineId = "parameter.compliance",
            Findings =
            [
                new ParameterComplianceFinding
                {
                    ValidationStage = validationStage,
                    ObjectId = objectId,
                    ObjectKind = "family",
                    ObjectName = objectName,
                    ParameterName = parameterName,
                    Passed = false,
                    Status = status,
                    Message = $"Required parameter '{parameterName}' is missing."
                }
            ]
        };
    }

    internal static NamingComplianceResult CreateInvalidNameResult(
        string objectId,
        string objectName,
        string validationStage = "prefix",
        string status = "MissingPrefix")
    {
        return new NamingComplianceResult
        {
            EngineId = "naming.compliance",
            Findings =
            [
                new NamingComplianceFinding
                {
                    ValidationStage = validationStage,
                    ObjectId = objectId,
                    ObjectKind = "family",
                    ObjectName = objectName,
                    Passed = false,
                    Status = status,
                    Message = $"Naming validation failed for '{objectName}'."
                }
            ]
        };
    }

    internal static IReadOnlyList<SharedParameterDefinition> CreateMvpSharedParameterDefinitions()
    {
        return
        [
            new SharedParameterDefinition { Name = "FireRating", Guid = FireRatingGuid, DataType = "TEXT", Group = "Data" },
            new SharedParameterDefinition { Name = "RoomName", Guid = RoomNameGuid, DataType = "TEXT", Group = "Identity Data" },
            new SharedParameterDefinition { Name = "AcousticRating", Guid = AcousticRatingGuid, DataType = "TEXT", Group = "Data" },
            new SharedParameterDefinition { Name = "Manufacturer", Guid = ManufacturerGuid, DataType = "TEXT", Group = "Data" }
        ];
    }

    internal static NamingPatternRule CreateDoorPatternRule()
    {
        return new NamingPatternRule
        {
            TokenizedPattern = "DR_{Token}",
            RegularExpression = @"^DR_[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }

    internal static NamingPatternRule CreateWindowPatternRule()
    {
        return new NamingPatternRule
        {
            TokenizedPattern = "WN_{Token}",
            RegularExpression = @"^WN_[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }

    internal static ParameterWriteRequestBuildRequest CreateParameterBuildRequest(
        ParameterComplianceResult complianceResult,
        ParameterTargetSet targetSet)
    {
        return new ParameterWriteRequestBuildRequest
        {
            ComplianceResult = complianceResult,
            TargetSet = targetSet,
            SharedParameterDefinitions = CreateMvpSharedParameterDefinitions(),
            RequestedAt = RequestedAt,
            RuleId = RuleId,
            CorrelationId = CorrelationId
        };
    }

    internal static TransactionRequest CreateTransaction(
        IReadOnlyList<WriteRequest> writeRequests,
        string transactionId,
        string name,
        string? description = null)
    {
        return new TransactionRequest
        {
            TransactionId = transactionId,
            Name = name,
            Description = description,
            WriteRequests = writeRequests,
            Scope = new TransactionScope
            {
                Kind = TransactionScopeKind.ModelScope,
                Metadata = new Dictionary<string, string> { ["workflow"] = "write-layer-e2e" }
            },
            Metadata = new Dictionary<string, string>
            {
                ["executionPolicy"] = "sequential",
                ["source"] = "write-layer-e2e"
            },
            CorrelationId = CorrelationId,
            RuleId = RuleId,
            Order = 1,
            RequestedAt = RequestedAt
        };
    }

    internal static WriteRequestBatch CreateWriteRequestBatch(
        IReadOnlyList<WriteRequest> writeRequests,
        string batchId)
    {
        return new WriteRequestBatch
        {
            BatchId = batchId,
            Requests = writeRequests,
            CorrelationId = CorrelationId,
            CreatedAt = RequestedAt,
            Metadata = new Dictionary<string, string> { ["source"] = "write-layer-e2e" }
        };
    }
}
