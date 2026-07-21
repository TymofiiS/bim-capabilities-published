using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Integration.Tests.Fixtures;

namespace BIMCapabilities.Integration.Tests;

public class Mvp001EndToEndValidationTests
{
    private readonly ValidationPipeline _pipeline = new();

    [Fact]
    public void Demo_bimrule_loads_and_executes_successfully()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DemoPass));

        Assert.True(result.LoadResult.Success);
        Assert.NotNull(result.LoadResult.Rule);
        Assert.Equal("STD-ARC-OPENINGS-V01", result.LoadResult.Rule!.Metadata.RuleId);
        Assert.True(result.RuleValidationSucceeded);
        Assert.Equal(ExecutionStatus.Completed, result.ExecutionResult!.Status);
        Assert.NotNull(result.ReportOutput);
        Assert.NotNull(result.HtmlReport);
        Assert.NotNull(result.JsonReport);
    }

    [Fact]
    public void Family_engine_executes_during_validation_pipeline()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DemoPass));

        Assert.NotNull(result.DoorTargetSetResult);
        Assert.NotNull(result.WindowTargetSetResult);
        Assert.Single(result.DoorTargetSetResult!.TargetSet.Families!);
        Assert.Single(result.WindowTargetSetResult!.TargetSet.Families!);
    }

    [Fact]
    public void Parameter_engine_executes_during_validation_pipeline()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DemoPass));

        Assert.NotNull(result.DoorParameterResult);
        Assert.NotNull(result.WindowParameterResult);
        Assert.Equal(100m, result.DoorParameterResult!.Summary!.CompliancePercentage);
        Assert.Equal(100m, result.WindowParameterResult!.Summary!.CompliancePercentage);
    }

    [Fact]
    public void Naming_engine_executes_during_validation_pipeline()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DemoPass));

        Assert.NotNull(result.DoorNamingResult);
        Assert.NotNull(result.WindowNamingResult);
        Assert.Equal(100m, result.DoorNamingResult!.Summary!.CompliancePercentage);
        Assert.Equal(100m, result.WindowNamingResult!.Summary!.CompliancePercentage);
    }

    [Fact]
    public void Evidence_is_collected_during_validation_pipeline()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DoorFail));

        Assert.NotNull(result.ExecutionResult!.Evidence);
        Assert.NotEmpty(result.ExecutionResult.Evidence!.Records);
    }

    [Fact]
    public void Html_report_is_generated()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DemoPass));

        Assert.NotNull(result.HtmlReport);
        Assert.Contains("<!DOCTYPE html>", result.HtmlReport!.Html);
        Assert.Contains("Openings Compliance Report", result.HtmlReport.Html);
        Assert.Equal("text/html; charset=utf-8", result.HtmlReport.ContentType);
    }

    [Fact]
    public void Json_report_is_generated()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DemoPass));

        Assert.NotNull(result.JsonReport);
        Assert.Contains("\"title\": \"Openings Compliance Report\"", result.JsonReport!.Json);
        Assert.Equal("application/json; charset=utf-8", result.JsonReport.ContentType);

        using var document = JsonDocument.Parse(result.JsonReport.Json);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    [Fact]
    public void Door_pass_scenario_achieves_full_compliance()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DoorPass));

        Assert.Equal(ExecutionStatus.Completed, result.ExecutionResult!.Status);
        Assert.Equal(100m, result.DoorParameterResult!.Summary!.CompliancePercentage);
        Assert.Equal(100m, result.DoorNamingResult!.Summary!.CompliancePercentage);
        Assert.Empty(result.DoorNamingResult.Evidence!);
    }

    [Fact]
    public void Door_fail_scenario_reports_naming_and_parameter_violations()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DoorFail));

        Assert.True(result.DoorNamingResult!.Summary!.FailedChecks > 0);
        Assert.True(result.DoorParameterResult!.Summary!.FailedChecks > 0);
        Assert.NotEmpty(result.DoorNamingResult.Evidence!);
        Assert.NotEmpty(result.DoorParameterResult.Evidence!);
    }

    [Fact]
    public void Window_pass_scenario_achieves_full_compliance()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.WindowPass));

        Assert.Equal(100m, result.WindowParameterResult!.Summary!.CompliancePercentage);
        Assert.Equal(100m, result.WindowNamingResult!.Summary!.CompliancePercentage);
    }

    [Fact]
    public void Window_fail_scenario_reports_naming_and_parameter_violations()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.WindowFail));

        Assert.True(result.WindowNamingResult!.Summary!.FailedChecks > 0);
        Assert.True(result.WindowParameterResult!.Summary!.FailedChecks > 0);
    }

    [Fact]
    public void Imported_cad_fail_scenario_collects_imported_cad_evidence()
    {
        var result = _pipeline.Execute(MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.ImportedCadFail));

        Assert.NotNull(result.DoorTargetSetResult!.Evidence);
        Assert.Contains(
            result.DoorTargetSetResult.Evidence!,
            record => record.Message.Contains("imported CAD", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.ExecutionResult!.Evidence!.Records,
            record => record.Severity is EvidenceSeverity.Error or EvidenceSeverity.Critical);
    }

    [Fact]
    public void Validation_pipeline_produces_deterministic_output()
    {
        var request = MvpValidationFixtureBuilder.CreateRequest(MvpValidationScenario.DemoPass);

        var first = _pipeline.Execute(request);
        var second = _pipeline.Execute(request);

        Assert.Equal(first.JsonReport!.Json, second.JsonReport!.Json);
        Assert.Equal(first.HtmlReport!.Html, second.HtmlReport!.Html);
    }
}

public class Mvp001ArchitectureTests
{
    [Fact]
    public void Composition_assembly_references_runtime_and_engines_but_not_launchers()
    {
        var referencedAssemblies = typeof(ValidationPipeline).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Engines.Family", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Engines.Parameter", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Engines.Naming", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Engines.Report", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain(referencedAssemblies, name => name!.StartsWith("Autodesk", StringComparison.Ordinal));
    }

    [Fact]
    public void Runtime_assembly_remains_free_of_engine_references()
    {
        var runtimeAssembly = typeof(BIMCapabilities.Runtime.RuntimeSkeleton).Assembly;
        var referencedAssemblies = runtimeAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Engines.Family", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Composition", referencedAssemblies);
    }

    [Fact]
    public void Validation_pipeline_does_not_expose_write_or_correction_methods()
    {
        var methods = typeof(ValidationPipeline).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

        Assert.All(methods, method =>
        {
            Assert.DoesNotContain("Write", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Transaction", method.Name, StringComparison.OrdinalIgnoreCase);
        });
    }
}
