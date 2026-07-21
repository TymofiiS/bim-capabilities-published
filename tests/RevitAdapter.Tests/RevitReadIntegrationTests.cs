using System.Reflection;
using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Tests.Fixtures;
using BIMCapabilities.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class RevitReadIntegrationTests
{
    private readonly RevitReadAdapter _adapter = new();

    [Fact]
    public void Revit_read_adapter_composes_all_required_services()
    {
        Assert.IsAssignableFrom<IRevitReadAdapter>(_adapter);
        Assert.IsAssignableFrom<IFamilyProvider>(_adapter.Families);
        Assert.IsAssignableFrom<IParameterProvider>(_adapter.Parameters);
        Assert.IsAssignableFrom<IRelationshipProvider>(_adapter.Relationships);
        Assert.IsAssignableFrom<IObjectTranslator>(_adapter.Translator);
    }

    [Fact]
    public void Family_query_flow_returns_normalized_stub_result()
    {
        var query = RevitReadIntegrationFixtures.CreateSampleFamilyQuery();

        var result = _adapter.Families.Retrieve(query);

        Assert.Single(result.Families);
        Assert.Equal("HTL_Door_01", result.Families[0].Name);
        Assert.Equal(CorrelationId, result.QueryMetadata!.CorrelationId);
        Assert.Equal("revit-adapter-read-skeleton", result.QueryMetadata.ProviderId);
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Code == "FamilyProvider.NotImplemented");
    }

    [Fact]
    public void Parameter_query_flow_returns_normalized_stub_result()
    {
        var query = RevitReadIntegrationFixtures.CreateSampleParameterQuery();

        var result = _adapter.Parameters.Retrieve(query);

        Assert.Equal(4, result.Parameters.Count);
        Assert.Contains(result.Parameters, parameter => parameter.Name == "FireRating");
        Assert.Contains(result.Parameters, parameter => parameter.Name == "Manufacturer");
        Assert.True(result.Parameters.Single(parameter => parameter.Name == "FireRating").IsSharedParameter);
        Assert.Equal(CorrelationId, result.QueryMetadata!.CorrelationId);
    }

    [Fact]
    public void Relationship_query_flow_returns_normalized_stub_result()
    {
        var query = RevitReadIntegrationFixtures.CreateSampleRelationshipQuery();

        var result = _adapter.Relationships.Retrieve(query);

        Assert.Equal(4, result.Relationships.Count);
        Assert.Contains(result.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.NestedFamily.ToString());
        Assert.Contains(result.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.ImportedCad.ToString());
        Assert.Equal(CorrelationId, result.QueryMetadata!.CorrelationId);
    }

    [Fact]
    public void Translation_flow_returns_normalized_family_and_object_results()
    {
        var familyResult = _adapter.Translator.Translate(RevitReadIntegrationFixtures.CreateSampleFamilyTranslationQuery());
        var elementResult = _adapter.Translator.Translate(RevitReadIntegrationFixtures.CreateSampleElementTranslationQuery());

        Assert.NotNull(familyResult.Family);
        Assert.Equal("family-001", familyResult.Family!.Identity.Id);
        Assert.NotNull(elementResult.Object);
        Assert.Equal("element-001", elementResult.Object!.Identity.Id);
    }

    [Fact]
    public void Mock_result_flow_returns_deterministic_stub_responses()
    {
        var familyQuery = RevitReadIntegrationFixtures.CreateSampleFamilyQuery();
        var first = _adapter.Families.Retrieve(familyQuery);
        var second = _adapter.Families.Retrieve(familyQuery);

        Assert.Equal(first.Families[0].Identity.Id, second.Families[0].Identity.Id);
        Assert.Equal(first.QueryMetadata!.ExecutedAt, second.QueryMetadata!.ExecutedAt);
    }

    [Fact]
    public void Adapter_composition_supports_end_to_end_read_workflow()
    {
        var familyResult = _adapter.Families.Retrieve(RevitReadIntegrationFixtures.CreateSampleFamilyQuery());
        var parameterResult = _adapter.Parameters.Retrieve(RevitReadIntegrationFixtures.CreateSampleParameterQuery());
        var relationshipResult = _adapter.Relationships.Retrieve(RevitReadIntegrationFixtures.CreateSampleRelationshipQuery());
        var translationResult = _adapter.Translator.Translate(RevitReadIntegrationFixtures.CreateSampleFamilyTranslationQuery());

        Assert.Equal(familyResult.Families[0].Identity.Id, translationResult.Family!.Identity.Id);
        Assert.Contains(parameterResult.Parameters, parameter => parameter.Name == "FireRating");
        Assert.Contains(relationshipResult.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.Dependency.ToString());
    }

    private const string CorrelationId = RevitReadIntegrationFixtures.CorrelationId;
}

public class RevitReadArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyNames =
    [
        "BIMCapabilities.Runtime",
        "BIMCapabilities.Engines.Family",
        "BIMCapabilities.Engines.Parameter",
        "BIMCapabilities.Engines.Naming",
        "BIMCapabilities.Engines.Report",
        "BIMCapabilities.Launchers.Revit"
    ];

    private static readonly string[] TestProjectForbiddenAssemblyNames =
    [
        "BIMCapabilities.Runtime",
        "BIMCapabilities.Engines.Report",
        "BIMCapabilities.Launchers.Revit"
    ];

    [Fact]
    public void Revit_adapter_assembly_references_only_contracts()
    {
        var adapterAssembly = typeof(RevitReadAdapter).Assembly;
        var referencedProjectAssemblies = adapterAssembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name!.StartsWith("BIMCapabilities.", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal(["BIMCapabilities.Contracts"], referencedProjectAssemblies);
    }

    [Fact]
    public void Revit_adapter_may_reference_revit_api_for_translation()
    {
        var referencedAssemblies = typeof(RevitReadAdapter).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(referencedAssemblies, name => name!.StartsWith("RevitAPI", StringComparison.Ordinal));
    }

    [Fact]
    public void Revit_adapter_does_not_reference_forbidden_assemblies()
    {
        var referencedAssemblies = typeof(RevitReadAdapter).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }
    }

    [Fact]
    public void Translation_layer_does_not_contain_business_or_compliance_logic()
    {
        var translationTypes = typeof(RevitObjectTranslator).Assembly.GetTypes()
            .Where(type => type.Namespace is not null &&
                           type.Namespace.StartsWith("BIMCapabilities.Adapters.Revit.Translation", StringComparison.Ordinal))
            .Where(type => type.IsClass && !type.IsAbstract);

        Assert.All(translationTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Validate", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Compliance", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Revit_read_skeleton_does_not_contain_collectors_or_query_execution()
    {
        var readTypes = typeof(RevitReadAdapter).Assembly.GetTypes()
            .Where(type => type.Namespace is "BIMCapabilities.Adapters.Revit.Read" or "BIMCapabilities.Adapters.Revit.Translation")
            .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("Skeleton", StringComparison.Ordinal));

        Assert.All(readTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);

            Assert.All(methods, method =>
            {
                Assert.DoesNotContain("Collector", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Filter", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ExecuteQuery", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Discover", method.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Traverse", method.Name, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void Revit_read_test_project_does_not_reference_runtime_report_or_launcher()
    {
        var referencedAssemblies = typeof(RevitReadIntegrationTests).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in TestProjectForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }

        Assert.Contains("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Contracts", referencedAssemblies);
    }
}
