using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Tests;

public class EvidenceTests
{
    private static readonly JsonSerializerOptions JsonOptions = EvidenceSerialization.Options;

    [Fact]
    public void Evidence_contracts_are_data_only_types()
    {
        var evidenceTypes = typeof(EvidenceRecord).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(EvidenceRecord).Namespace);

        Assert.All(evidenceTypes, type =>
        {
            if (type == typeof(EvidenceSeverity) || type == typeof(EvidenceCategory))
            {
                return;
            }

            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void EvidenceRecord_can_be_constructed_with_required_properties()
    {
        var record = EvidenceTestData.CreateParameterValidationRecord();

        Assert.Equal("evidence-001", record.EvidenceId);
        Assert.Equal("parameter-engine", record.Source.EngineId);
        Assert.Equal("parameter.existence", record.Source.AtomId);
        Assert.Equal("door-001", record.Target!.TargetId);
        Assert.Equal(EvidenceCategory.Validation, record.Category);
        Assert.Equal(EvidenceSeverity.Error, record.Severity);
        Assert.Equal("FireRating", record.StructuredData!["parameterName"]);
        Assert.Single(record.Attachments!);
    }

    [Fact]
    public void EvidenceRecord_supports_json_round_trip_serialization()
    {
        var original = EvidenceTestData.CreateParameterValidationRecord();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<EvidenceRecord>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.EvidenceId, roundTrip.EvidenceId);
        Assert.Equal(original.Source.EngineId, roundTrip.Source.EngineId);
        Assert.Equal(original.Target!.TargetName, roundTrip.Target!.TargetName);
        Assert.Equal(original.Category, roundTrip.Category);
        Assert.Equal(original.Severity, roundTrip.Severity);
        Assert.Equal(original.Message, roundTrip.Message);
        Assert.Equal(original.StructuredData!["actualState"], roundTrip.StructuredData!["actualState"]);
        Assert.Equal(original.Attachments![0].FileName, roundTrip.Attachments![0].FileName);
    }

    [Fact]
    public void EvidenceCollection_can_hold_multiple_records()
    {
        var collection = EvidenceTestData.CreateDemoCollection();

        Assert.Equal("collection-001", collection.CollectionId);
        Assert.Equal("corr-001", collection.CorrelationId);
        Assert.Equal(2, collection.Records.Count);
        Assert.Contains(collection.Records, record => record.Category == EvidenceCategory.Validation);
        Assert.Contains(collection.Records, record => record.Category == EvidenceCategory.Compliance);
    }

    [Fact]
    public void EvidenceCollection_supports_json_round_trip_serialization()
    {
        var original = EvidenceTestData.CreateDemoCollection();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<EvidenceCollection>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.CollectionId, roundTrip.CollectionId);
        Assert.Equal(original.Records.Count, roundTrip.Records.Count);
        Assert.Equal(original.Records[0].EvidenceId, roundTrip.Records[0].EvidenceId);
        Assert.Equal(original.Records[1].Severity, roundTrip.Records[1].Severity);
    }

    [Theory]
    [InlineData(EvidenceSeverity.Info)]
    [InlineData(EvidenceSeverity.Warning)]
    [InlineData(EvidenceSeverity.Error)]
    [InlineData(EvidenceSeverity.Critical)]
    public void EvidenceSeverity_supports_required_values(EvidenceSeverity severity)
    {
        var record = new EvidenceRecord
        {
            EvidenceId = "severity-test",
            Source = new EvidenceSource { EngineId = "report-engine" },
            Category = EvidenceCategory.Audit,
            Severity = severity,
            Message = "Severity test."
        };

        Assert.Equal(severity, record.Severity);
    }

    [Theory]
    [InlineData(EvidenceCategory.Validation)]
    [InlineData(EvidenceCategory.Fix)]
    [InlineData(EvidenceCategory.Compliance)]
    [InlineData(EvidenceCategory.Audit)]
    [InlineData(EvidenceCategory.KnowledgeGap)]
    [InlineData(EvidenceCategory.Optimization)]
    public void EvidenceCategory_supports_required_values(EvidenceCategory category)
    {
        var record = new EvidenceRecord
        {
            EvidenceId = "category-test",
            Source = new EvidenceSource { EngineId = "family-engine" },
            Category = category,
            Severity = EvidenceSeverity.Info,
            Message = "Category test."
        };

        Assert.Equal(category, record.Category);
    }

    [Fact]
    public void EvidenceSource_and_EvidenceTarget_required_properties_can_be_populated()
    {
        var source = new EvidenceSource
        {
            EngineId = "family-engine",
            AtomId = "family.imported-cad",
            RuleId = "STD-ARC-OPENINGS-V01",
            CapabilityId = "family.imported-cad"
        };

        var target = new EvidenceTarget
        {
            TargetType = "Family",
            TargetId = "family-001",
            TargetName = "HTL_Door_01",
            TargetSetDescription = "All Curtain-Wall Families"
        };

        Assert.Equal("family-engine", source.EngineId);
        Assert.Equal("Family", target.TargetType);
        Assert.Equal("HTL_Door_01", target.TargetName);
    }

    [Fact]
    public void EvidenceAttachment_required_properties_can_be_populated()
    {
        var attachment = new EvidenceAttachment
        {
            AttachmentId = "attachment-002",
            ContentType = "application/json",
            FileName = "before-after.json",
            Content = """{"before":"DR_001","after":"DR_HT_001"}""",
            Uri = "file:///evidence/before-after.json"
        };

        Assert.Equal("application/json", attachment.ContentType);
        Assert.NotNull(attachment.Content);
        Assert.NotNull(attachment.Uri);
    }

    [Fact]
    public void Evidence_contracts_do_not_reference_runtime_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(EvidenceRecord).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }
}
