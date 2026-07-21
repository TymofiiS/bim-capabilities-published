using System.Reflection;
using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Adapters.Revit.Tests.Fixtures.EndToEnd;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Adapters.Revit.Tests;

public class RevitAdapterReadEndToEndTests
{
    private readonly RevitAdapter _adapter = RevitAdapterEndToEndFixtureBuilder.CreateAdapter();

    [Fact]
    public void Adapter_composes_family_parameter_relationship_and_translation_services()
    {
        Assert.IsAssignableFrom<IRevitAdapter>(_adapter);
        Assert.IsAssignableFrom<IRevitReadAdapter>(_adapter);
        Assert.IsAssignableFrom<IFamilyProvider>(_adapter.Families);
        Assert.IsAssignableFrom<IParameterProvider>(_adapter.Parameters);
        Assert.IsAssignableFrom<IRelationshipProvider>(_adapter.Relationships);
        Assert.IsAssignableFrom<IObjectTranslator>(_adapter.Translator);
    }

    [Fact]
    public void Door_retrieval_workflow_returns_normalized_families_and_translation()
    {
        var result = DoorFixture.CreateAdapter().Read(DoorFixture.CreateContext());

        Assert.NotNull(result.Families);
        Assert.Single(result.Families!.Families);
        Assert.Equal("HTL_Door_01", result.Families.Families[0].Name);
        Assert.Equal("Doors", result.Families.Families[0].Category!.Name);
        Assert.NotNull(result.Translations);
        Assert.Equal(DoorFixture.CreateContext().TranslationQueries!.Count, result.Translations!.Count);
        Assert.Equal(DoorFamilyId, result.Translations![0].Family!.Identity.Id);
        Assert.Equal(1, result.Statistics!.FamiliesRetrieved);
        Assert.Equal(1, result.Statistics.TranslationsRetrieved);
    }

    [Fact]
    public void Window_retrieval_workflow_returns_normalized_families_and_translation()
    {
        var result = WindowFixture.CreateAdapter().Read(WindowFixture.CreateContext());

        Assert.NotNull(result.Families);
        Assert.Single(result.Families!.Families);
        Assert.Equal("HTL_Window_01", result.Families.Families[0].Name);
        Assert.Equal("Windows", result.Families.Families[0].Category!.Name);
        Assert.Equal(WindowFamilyId, result.Translations![0].Family!.Identity.Id);
    }

    [Fact]
    public void Parameter_retrieval_workflow_returns_mvp_parameters()
    {
        var result = _adapter.Read(RevitAdapterEndToEndFixtureBuilder.CreateParameterScenarioContext());

        Assert.NotNull(result.Parameters);
        Assert.Equal(4, result.Parameters!.Parameters.Count);
        Assert.Contains(result.Parameters.Parameters, parameter => parameter.Name == "FireRating");
        Assert.Contains(result.Parameters.Parameters, parameter => parameter.Name == "RoomName");
        Assert.Contains(result.Parameters.Parameters, parameter => parameter.Name == "AcousticRating");
        Assert.Contains(result.Parameters.Parameters, parameter => parameter.Name == "Manufacturer");
        Assert.Equal(4, result.Statistics!.ParametersRetrieved);
    }

    [Fact]
    public void Relationship_retrieval_workflow_returns_nested_imported_cad_and_dependency_relationships()
    {
        var result = _adapter.Read(RevitAdapterEndToEndFixtureBuilder.CreateRelationshipScenarioContext());

        Assert.NotNull(result.Relationships);
        Assert.Equal(3, result.Relationships!.Relationships.Count);
        Assert.Contains(result.Relationships.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.NestedFamily.ToString());
        Assert.Contains(result.Relationships.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.ImportedCad.ToString());
        Assert.Contains(result.Relationships.Relationships, relationship =>
            relationship.Metadata!["queryRelationshipType"] == RelationshipType.Dependency.ToString());
        Assert.Equal(3, result.Statistics!.RelationshipsRetrieved);
    }

    [Fact]
    public void Imported_CAD_workflow_returns_imported_cad_relationship()
    {
        var result = ImportedCadFixture.CreateAdapter().Read(ImportedCadFixture.CreateContext());

        var relationship = Assert.Single(result.Relationships!.Relationships);
        Assert.Equal("imported-cad-door-001", relationship.Target.Id);
        Assert.Equal("importedCad", relationship.Target.Kind);
        Assert.Equal(RelationshipType.ImportedCad.ToString(), relationship.Metadata!["queryRelationshipType"]);
    }

    [Fact]
    public void Nested_family_workflow_returns_nested_family_relationship()
    {
        var result = NestedFamilyFixture.CreateAdapter().Read(NestedFamilyFixture.CreateContext());

        var relationship = Assert.Single(result.Relationships!.Relationships);
        Assert.Equal(NormalizedRelationshipType.Nested, relationship.RelationshipType);
        Assert.Equal("nested-family-door-001", relationship.Target.Id);
        Assert.Equal(RelationshipType.NestedFamily.ToString(), relationship.Metadata!["queryRelationshipType"]);
    }

    [Fact]
    public void Mixed_family_workflow_returns_door_and_window_families()
    {
        var result = MixedFamilyFixture.CreateAdapter().Read(MixedFamilyFixture.CreateContext());

        Assert.Equal(2, result.Families!.Families.Count);
        Assert.Contains(result.Families.Families, family => family.Category!.Name == "Doors");
        Assert.Contains(result.Families.Families, family => family.Category!.Name == "Windows");
    }

    [Fact]
    public void Large_dataset_workflow_returns_expected_family_and_parameter_counts()
    {
        var result = LargeFixture.CreateAdapter().Read(LargeFixture.CreateContext());

        Assert.Equal(LargeFixture.FamilyCount, result.Families!.Families.Count);
        Assert.Equal(LargeFixture.FamilyCount, result.Parameters!.Parameters.Count);
        Assert.Equal(LargeFixture.FamilyCount, result.Statistics!.FamiliesRetrieved);
        Assert.Equal(LargeFixture.FamilyCount, result.Statistics.ParametersRetrieved);
    }

    [Fact]
    public void Complete_read_workflow_integrates_all_retrieval_layers()
    {
        var result = _adapter.Read(RevitAdapterEndToEndFixtureBuilder.CreateCompleteWorkflowContext());

        Assert.NotNull(result.Families);
        Assert.NotNull(result.Parameters);
        Assert.NotNull(result.Relationships);
        Assert.NotNull(result.Translations);
        Assert.Equal(RevitAdapterEndToEndFixtureBuilder.CorrelationId, result.Metadata!.CorrelationId);
        Assert.Equal(RevitAdapterReadSupport.AdapterId, result.Metadata.AdapterId);
        Assert.True(result.Statistics!.FamiliesRetrieved > 0);
        Assert.True(result.Statistics.ParametersRetrieved > 0);
        Assert.True(result.Statistics.RelationshipsRetrieved > 0);
        Assert.True(result.Statistics.TranslationsRetrieved > 0);
    }

    [Fact]
    public void Statistics_generation_populates_aggregate_counts()
    {
        var result = _adapter.Read(RevitAdapterEndToEndFixtureBuilder.CreateCompleteWorkflowContext());

        Assert.NotNull(result.Statistics);
        Assert.NotNull(result.Statistics!.Properties);
        Assert.Equal("true", result.Statistics.Properties!["includesFamilyQuery"]);
        Assert.Equal("true", result.Statistics.Properties["includesParameterQuery"]);
        Assert.Equal("true", result.Statistics.Properties["includesRelationshipQuery"]);
    }

    [Fact]
    public void Diagnostic_generation_preserves_provider_diagnostics()
    {
        var result = _adapter.Read(new RevitAdapterReadContext
        {
            CorrelationId = RevitAdapterEndToEndFixtureBuilder.CorrelationId,
            ParameterQuery = new ParameterQuery
            {
                ParameterNames = ["MissingParameter"],
                Scope = new ParameterQueryScope { Kind = ParameterQueryScopeKind.EntireModel }
            }
        });

        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!, diagnostic => diagnostic.Source == "provider:parameter");
    }

    [Fact]
    public void Translation_correctness_matches_provider_and_translator_output()
    {
        var familyQuery = new FamilyQuery
        {
            FamilyNames = ["HTL_Door_01"],
            Scope = new FamilyQueryScope { Kind = FamilyQueryScopeKind.EntireModel },
            CorrelationId = RevitAdapterEndToEndFixtureBuilder.CorrelationId
        };

        var translationQuery = new ObjectTranslationQuery
        {
            SourceObjectId = DoorFamilyId,
            SourceKind = "family",
            CorrelationId = RevitAdapterEndToEndFixtureBuilder.CorrelationId
        };

        var familyResult = _adapter.Families.Retrieve(familyQuery);
        var translationResult = _adapter.Translator.Translate(translationQuery);
        var readResult = _adapter.Read(new RevitAdapterReadContext
        {
            CorrelationId = RevitAdapterEndToEndFixtureBuilder.CorrelationId,
            FamilyQuery = familyQuery,
            TranslationQueries = [translationQuery]
        });

        Assert.Equal(familyResult.Families[0].Identity.Id, readResult.Families!.Families[0].Identity.Id);
        Assert.Equal(translationResult.Family!.Identity.Id, readResult.Translations![0].Family!.Identity.Id);
    }

    [Fact]
    public void Read_output_is_deterministic_for_same_context()
    {
        var context = RevitAdapterEndToEndFixtureBuilder.CreateCompleteWorkflowContext();

        var first = _adapter.Read(context);
        var second = _adapter.Read(context);

        Assert.Equal(
            first.Families!.Families.Select(family => family.Identity.Id),
            second.Families!.Families.Select(family => family.Identity.Id));
        Assert.Equal(
            first.Parameters!.Parameters.Select(parameter => parameter.Identifier.Id),
            second.Parameters!.Parameters.Select(parameter => parameter.Identifier.Id));
        Assert.Equal(
            first.Relationships!.Relationships.Select(relationship => relationship.Target.Id),
            second.Relationships!.Relationships.Select(relationship => relationship.Target.Id));
        Assert.Equal(first.Metadata!.ExecutedAt, second.Metadata!.ExecutedAt);
    }

    [Fact]
    public void Read_throws_when_context_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => _adapter.Read(null!));
    }

    private const string DoorFamilyId = RevitAdapterEndToEndFixtureBuilder.DoorFamilyId;

    private const string WindowFamilyId = RevitAdapterEndToEndFixtureBuilder.WindowFamilyId;
}

public class RevitAdapterReadEndToEndArchitectureTests
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

    [Fact]
    public void Revit_adapter_does_not_reference_forbidden_engine_or_runtime_assemblies()
    {
        var referencedAssemblies = typeof(RevitAdapter).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in ForbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
        }
    }

    [Fact]
    public void Revit_adapter_read_composition_does_not_contain_business_or_compliance_logic()
    {
        var compositionTypes = typeof(RevitAdapter).Assembly.GetTypes()
            .Where(type => type == typeof(RevitAdapter) || type == typeof(RevitAdapterReadSupport))
            .Where(type => type.IsClass);

        Assert.All(compositionTypes, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
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
    public void Revit_adapter_test_project_does_not_reference_forbidden_assemblies()
    {
        var referencedAssemblies = typeof(RevitAdapterReadEndToEndTests).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Report", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Contracts", referencedAssemblies);
    }
}
