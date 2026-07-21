using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Reports.Output;

namespace BIMCapabilities.Contracts.Tests;

public class ReportOutputTests
{
    private static readonly JsonSerializerOptions JsonOptions = ReportOutputSerialization.Options;

    [Fact]
    public void Report_output_contracts_are_data_only_types()
    {
        var outputTypes = typeof(ReportOutput).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ReportOutput).Namespace);

        Assert.All(outputTypes, type =>
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void ReportOutput_can_be_constructed_with_required_properties()
    {
        var output = ReportOutputTestData.CreateComplianceReportOutput();

        Assert.Equal("report-001", output.ReportId);
        Assert.Equal("Openings Compliance Report", output.Title);
        Assert.Equal("compliance-report-v1", output.ProfileId);
        Assert.Equal(2, output.Sections.Count);
        Assert.Equal("STD-ARC-OPENINGS-V01", output.Metadata!.RuleId);
        Assert.Equal(new DateTimeOffset(2026, 6, 19, 20, 0, 0, TimeSpan.Zero), output.GeneratedAt);
    }

    [Fact]
    public void ReportOutput_supports_json_round_trip_serialization()
    {
        var original = ReportOutputTestData.CreateComplianceReportOutput();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ReportOutput>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.ReportId, roundTrip.ReportId);
        Assert.Equal(original.Title, roundTrip.Title);
        Assert.Equal(original.ProfileId, roundTrip.ProfileId);
        Assert.Equal(original.Sections.Count, roundTrip.Sections.Count);
        Assert.Equal(original.Metadata!.CorrelationId, roundTrip.Metadata!.CorrelationId);
        Assert.Equal(original.GeneratedAt, roundTrip.GeneratedAt);
    }

    [Fact]
    public void ReportSection_supports_required_section_fields()
    {
        var section = ReportOutputTestData.CreateSection("Evidence", order: 3, required: false);

        Assert.Equal("Evidence", section.Name);
        Assert.Equal(3, section.Order);
        Assert.False(section.Required);
        Assert.Equal("Evidence section.", section.Description);
    }

    [Fact]
    public void ReportContent_supports_text_structured_data_and_references()
    {
        var output = ReportOutputTestData.CreateComplianceReportOutput();
        var summaryContent = output.Sections[0].Content!;

        Assert.Equal("Overall compliance status: Fail.", summaryContent.Text);
        Assert.Equal("Fail", summaryContent.StructuredData!["overallStatus"]);
        Assert.Single(summaryContent.EvidenceReferences!);
        Assert.Single(summaryContent.DiagnosticReferences!);
        Assert.Equal("Evidence", summaryContent.EvidenceReferences![0].ReferenceType);
    }

    [Fact]
    public void ReportMetadata_required_properties_can_be_populated()
    {
        var metadata = new ReportMetadata
        {
            RuleId = "STD-ARC-OPENINGS-V01",
            ProfileId = "validation-report-v1",
            CorrelationId = "corr-002",
            GeneratedBy = "ReportEngine",
            Properties = new Dictionary<string, string>
            {
                ["renderer"] = "unspecified"
            }
        };

        Assert.Equal("validation-report-v1", metadata.ProfileId);
        Assert.Equal("unspecified", metadata.Properties!["renderer"]);
    }

    [Fact]
    public void ReportReference_required_properties_can_be_populated()
    {
        var reference = new ReportReference
        {
            ReferenceType = "Evidence",
            ReferenceId = "evidence-010",
            Description = "Naming validation evidence."
        };

        Assert.Equal("evidence-010", reference.ReferenceId);
    }

    [Fact]
    public void ReportAttachment_required_properties_can_be_populated()
    {
        var output = ReportOutputTestData.CreateComplianceReportOutput();
        var attachment = output.Sections[1].Content!.Attachments![0];

        Assert.Equal("attachment-001", attachment.AttachmentId);
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal("violations.txt", attachment.FileName);
        Assert.Equal("Violation details.", attachment.Content);
    }

    [Fact]
    public void Report_output_contracts_do_not_reference_runtime_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ReportOutput).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Report", referencedAssemblies);
    }
}
