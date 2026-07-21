using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Parameter;
using BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Engines.Parameter.Tests.Fixtures;

internal static class ParameterWriteRequestBuilderTestData
{
    internal const string RuleId = "STD-ARC-OPENINGS-V01";
    internal const string CorrelationId = "corr-parameter-write-builder-001";
    internal static readonly DateTimeOffset RequestedAt = new(2026, 6, 20, 17, 0, 0, TimeSpan.Zero);

    internal const string FireRatingGuid = "f1a2b3c4-d5e6-7890-abcd-ef1234567890";
    internal const string RoomNameGuid = "b2c3d4e5-f6a7-8901-bcde-f12345678902";
    internal const string AcousticRatingGuid = "a1b2c3d4-e5f6-7890-abcd-ef1234567891";
    internal const string ManufacturerGuid = "c3d4e5f6-a7b8-9012-cdef-123456789013";

    internal static IReadOnlyList<SharedParameterDefinition> CreateMvpSharedParameterDefinitions()
    {
        return
        [
            new SharedParameterDefinition
            {
                Name = "FireRating",
                Guid = FireRatingGuid,
                DataType = "TEXT",
                Group = "Data"
            },
            new SharedParameterDefinition
            {
                Name = "RoomName",
                Guid = RoomNameGuid,
                DataType = "TEXT",
                Group = "Identity Data"
            },
            new SharedParameterDefinition
            {
                Name = "AcousticRating",
                Guid = AcousticRatingGuid,
                DataType = "TEXT",
                Group = "Data"
            },
            new SharedParameterDefinition
            {
                Name = "Manufacturer",
                Guid = ManufacturerGuid,
                DataType = "TEXT",
                Group = "Data"
            }
        ];
    }

    internal static ParameterTargetSet CreateDoorTargetSet()
    {
        return new ParameterTargetSet
        {
            TargetSetId = "target-set-doors-001",
            TargetFamilies =
            [
                new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier
                    {
                        Id = "family-001",
                        Kind = "family",
                        Scope = "project-document"
                    },
                    Name = "HTL_Door_01"
                }
            ]
        };
    }

    internal static ParameterWriteRequestBuildRequest CreateBuildRequest(
        ParameterComplianceResult complianceResult,
        ParameterTargetSet? targetSet = null,
        IReadOnlyList<ParameterWriteCorrectionIntent>? correctionIntents = null,
        IReadOnlyDictionary<string, bool>? parameterBindings = null)
    {
        return new ParameterWriteRequestBuildRequest
        {
            ComplianceResult = complianceResult,
            TargetSet = targetSet ?? CreateDoorTargetSet(),
            SharedParameterDefinitions = CreateMvpSharedParameterDefinitions(),
            CorrectionIntents = correctionIntents,
            ParameterBindings = parameterBindings,
            RequestedAt = RequestedAt,
            RuleId = RuleId,
            CorrelationId = CorrelationId
        };
    }

    internal static ParameterComplianceResult CreateMissingParameterComplianceResult(string parameterName)
    {
        return new ParameterComplianceResult
        {
            EngineId = "parameter.compliance",
            Findings =
            [
                new ParameterComplianceFinding
                {
                    ValidationStage = "existence",
                    ObjectId = "family-001",
                    ObjectKind = "family",
                    ObjectName = "HTL_Door_01",
                    ParameterName = parameterName,
                    Passed = false,
                    Status = "Missing",
                    Message = $"Required parameter '{parameterName}' is missing."
                }
            ]
        };
    }

    internal static ParameterComplianceResult CreateInvalidValueComplianceResult(string parameterName)
    {
        return new ParameterComplianceResult
        {
            EngineId = "parameter.compliance",
            Findings =
            [
                new ParameterComplianceFinding
                {
                    ValidationStage = "value",
                    ObjectId = "family-001",
                    ObjectKind = "family",
                    ObjectName = "HTL_Door_01",
                    ParameterName = parameterName,
                    Passed = false,
                    Status = "InvalidValue",
                    Message = $"Parameter '{parameterName}' has an invalid value."
                }
            ]
        };
    }

    internal static ParameterComplianceResult CreateInstanceMissingValueComplianceResult(
        string parameterName,
        string instanceId,
        string familyName)
    {
        return new ParameterComplianceResult
        {
            EngineId = "parameter.compliance",
            Findings =
            [
                new ParameterComplianceFinding
                {
                    ValidationStage = "value",
                    ObjectId = instanceId,
                    ObjectKind = "familyInstance",
                    ObjectName = "600 x 900mm",
                    ParameterName = parameterName,
                    Passed = false,
                    Status = "MissingValue",
                    Message = $"Parameter '{parameterName}' is missing a required value."
                }
            ]
        };
    }

    internal static ParameterTargetSet CreateWindowTargetSetWithInstance(string instanceId)
    {
        return new ParameterTargetSet
        {
            TargetSetId = "target-set-windows-001",
            TargetFamilies =
            [
                new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier
                    {
                        Id = "family-window-001",
                        Kind = "family",
                        Scope = "project-document"
                    },
                    Name = "M_Window-Fixed"
                }
            ],
            TargetInstances =
            [
                new NormalizedPlacedInstance
                {
                    Identity = new NormalizedIdentifier
                    {
                        Id = instanceId,
                        Kind = "familyInstance",
                        Scope = "project-document"
                    },
                    Name = "600 x 900mm",
                    FamilyName = "M_Window-Fixed",
                    FamilyTypeName = "600 x 900mm",
                    CategoryName = "Windows",
                    Parameters = [new NormalizedParameter
                    {
                        Identifier = new NormalizedIdentifier { Id = "parameter-my_room", Kind = "parameter" },
                        Name = "MY_Room",
                        Value = string.Empty,
                        StorageType = NormalizedParameterStorageType.String
                    }]
                }
            ]
        };
    }

    internal static ParameterComplianceResult CreateMissingSharedParameterComplianceResult(string parameterName)
    {
        return new ParameterComplianceResult
        {
            EngineId = "parameter.compliance",
            Findings =
            [
                new ParameterComplianceFinding
                {
                    ValidationStage = "shared-parameter",
                    ObjectId = "family-001",
                    ObjectKind = "family",
                    ObjectName = "HTL_Door_01",
                    ParameterName = parameterName,
                    Passed = false,
                    Status = "Missing",
                    Message = $"Shared parameter '{parameterName}' failed validation."
                }
            ]
        };
    }
}
