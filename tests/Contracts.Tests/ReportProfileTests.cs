using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Reports.Profiles;

namespace BIMCapabilities.Contracts.Tests;

public class ReportProfileTests
{
    private static readonly JsonSerializerOptions JsonOptions = ReportProfileSerialization.Options;

    [Fact]
    public void Report_profile_contracts_are_data_only_types()
    {
        var profileTypes = typeof(ReportProfile).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ReportProfile).Namespace);

        Assert.All(profileTypes, type =>
        {
            if (type == typeof(ReportProfileType) || type.IsInterface)
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
    public void ReportProfile_can_be_constructed_with_required_properties()
    {
        var profile = ReportProfileTestData.CreateComplianceProfile();

        Assert.Equal("compliance-report-v1", profile.ProfileId);
        Assert.Equal(ReportProfileType.Compliance, profile.ProfileType);
        Assert.Equal(4, profile.Definition.Sections.Count);
        Assert.Equal("Compliance Summary", profile.Definition.Sections[0].Name);
        Assert.Equal("GroupBySeverity", profile.Definition.Configuration!.AggregationStrategy);
    }

    [Fact]
    public void ReportProfile_supports_json_round_trip_serialization()
    {
        var original = ReportProfileTestData.CreateComplianceProfile();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<ReportProfile>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.ProfileId, roundTrip.ProfileId);
        Assert.Equal(original.ProfileType, roundTrip.ProfileType);
        Assert.Equal(original.Definition.Sections.Count, roundTrip.Definition.Sections.Count);
        Assert.Equal(original.Definition.Sections[1].Required, roundTrip.Definition.Sections[1].Required);
        Assert.Equal(original.Definition.Configuration!.SummaryStrategy, roundTrip.Definition.Configuration!.SummaryStrategy);
    }

    [Theory]
    [InlineData(ReportProfileType.Compliance)]
    [InlineData(ReportProfileType.Validation)]
    [InlineData(ReportProfileType.Fix)]
    [InlineData(ReportProfileType.Audit)]
    [InlineData(ReportProfileType.KnowledgeGap)]
    [InlineData(ReportProfileType.Optimization)]
    public void ReportProfileType_supports_approved_profile_types(ReportProfileType profileType)
    {
        var profile = new ReportProfile
        {
            ProfileId = $"{profileType}-profile",
            Name = profileType.ToString(),
            ProfileType = profileType,
            Definition = ReportProfileTestData.CreateDefinition(profileType, [])
        };

        Assert.Equal(profileType, profile.ProfileType);
        Assert.Contains(profileType, ReportProfileTestData.ApprovedProfileTypes());
    }

    [Fact]
    public void ReportProfileSection_supports_required_section_fields()
    {
        var section = ReportProfileTestData.CreateSection(
            "Validation Summary",
            required: true,
            order: 1,
            description: "Summarizes validation execution results.");

        Assert.Equal("Validation Summary", section.Name);
        Assert.True(section.Required);
        Assert.Equal(1, section.Order);
        Assert.Equal("Summarizes validation execution results.", section.Description);
    }

    [Fact]
    public void ReportProfileDefinition_orders_sections_by_order_value()
    {
        var definition = ReportProfileTestData.CreateDefinition(
            ReportProfileType.Validation,
            [
                ReportProfileTestData.CreateSection("Fail Results", required: true, order: 3),
                ReportProfileTestData.CreateSection("Validation Summary", required: true, order: 1),
                ReportProfileTestData.CreateSection("Pass Results", required: true, order: 2)
            ]);

        var orderedSections = definition.Sections.OrderBy(section => section.Order).Select(section => section.Name).ToArray();

        Assert.Equal(["Validation Summary", "Pass Results", "Fail Results"], orderedSections);
    }

    [Fact]
    public void ReportProfileConfiguration_can_describe_reporting_intent_without_output_format()
    {
        var configuration = new ReportProfileConfiguration
        {
            EvidenceSelectionStrategy = "IncludeValidationEvidence",
            AggregationStrategy = "GroupByEngine",
            SummaryStrategy = "ValidationSummary",
            PresentationIntent = "SeparatePassAndFailResults",
            Metadata = new Dictionary<string, string>
            {
                ["outputFormat"] = "unspecified"
            }
        };

        Assert.Equal("IncludeValidationEvidence", configuration.EvidenceSelectionStrategy);
        Assert.Equal("unspecified", configuration.Metadata!["outputFormat"]);
    }

    [Fact]
    public void Report_profile_contracts_do_not_reference_runtime_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(ReportProfile).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Report", referencedAssemblies);
    }
}
