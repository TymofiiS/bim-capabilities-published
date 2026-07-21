using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Tests;

internal static class EvidenceTestData
{
    internal static EvidenceRecord CreateParameterValidationRecord()
    {
        return new EvidenceRecord
        {
            EvidenceId = "evidence-001",
            Timestamp = new DateTimeOffset(2026, 6, 19, 14, 30, 0, TimeSpan.Zero),
            Source = new EvidenceSource
            {
                EngineId = "parameter-engine",
                AtomId = "parameter.existence",
                RuleId = "STD-ARC-OPENINGS-V01",
                CapabilityId = "parameter.existence"
            },
            Target = new EvidenceTarget
            {
                TargetType = "Element",
                TargetId = "door-001",
                TargetName = "DR_HT_001",
                TargetSetDescription = "All Doors"
            },
            Category = EvidenceCategory.Validation,
            Severity = EvidenceSeverity.Error,
            Message = "Required shared parameter 'FireRating' is missing.",
            StructuredData = new Dictionary<string, string>
            {
                ["parameterName"] = "FireRating",
                ["expectedState"] = "Present",
                ["actualState"] = "Missing",
                ["diagnosticCode"] = "ParameterMissing"
            },
            Attachments =
            [
                new EvidenceAttachment
                {
                    AttachmentId = "attachment-001",
                    ContentType = "text/plain",
                    FileName = "parameter-detail.txt",
                    Content = "Parameter FireRating was not found on target element."
                }
            ]
        };
    }

    internal static EvidenceCollection CreateDemoCollection()
    {
        return new EvidenceCollection
        {
            CollectionId = "collection-001",
            CorrelationId = "corr-001",
            Records =
            [
                CreateParameterValidationRecord(),
                new EvidenceRecord
                {
                    EvidenceId = "evidence-002",
                    Timestamp = new DateTimeOffset(2026, 6, 19, 14, 31, 0, TimeSpan.Zero),
                    Source = new EvidenceSource
                    {
                        EngineId = "naming-engine",
                        AtomId = "naming.prefix.validation",
                        RuleId = "STD-ARC-OPENINGS-V01",
                        CapabilityId = "naming.prefix.validation"
                    },
                    Target = new EvidenceTarget
                    {
                        TargetType = "Element",
                        TargetId = "door-001",
                        TargetName = "DR_HT_001"
                    },
                    Category = EvidenceCategory.Compliance,
                    Severity = EvidenceSeverity.Info,
                    Message = "Naming prefix validation passed."
                }
            ]
        };
    }
}
