using System.Reflection;
using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Contracts.Reports.Rendering;
using BIMCapabilities.Launchers.Revit.Execution;
using BIMCapabilities.Launchers.Revit.Results;
using BIMCapabilities.Launchers.Revit.Tests.Fixtures;

namespace BIMCapabilities.Launchers.Revit.Tests;

public class LauncherRuleExecutionServiceTests
{
    [Fact]
    public void Execute_loads_rule_and_runs_validation_pipeline()
    {
        var browser = new RecordingReportBrowserLauncher();
        var service = new LauncherRuleExecutionService(
            new ValidationPipeline(),
            new ReportOutputWriter(),
            browser);

        var result = service.Execute(LauncherTestFixtureBuilder.CreatePassRequest());

        Assert.Equal(LauncherRuleExecutionStatus.Completed, result.Status);
        Assert.True(result.PipelineResult!.RuleValidationSucceeded);
        Assert.NotNull(result.PipelineResult.DoorParameterResult);
        Assert.NotNull(result.PipelineResult.WindowParameterResult);
        Assert.NotNull(result.PipelineResult.DoorNamingResult);
        Assert.NotNull(result.PipelineResult.WindowNamingResult);
    }

    [Fact]
    public void Execute_generates_html_and_json_reports_in_temp_directory()
    {
        var service = CreateService(new RecordingReportBrowserLauncher());
        var result = service.Execute(LauncherTestFixtureBuilder.CreatePassRequest());

        Assert.NotNull(result.HtmlReportPath);
        Assert.NotNull(result.JsonReportPath);
        Assert.StartsWith(Path.Combine(Path.GetTempPath(), "BIMCapabilities"), result.ReportDirectory!);
        Assert.True(File.Exists(result.HtmlReportPath));
        Assert.True(File.Exists(result.JsonReportPath));
        Assert.Contains("<!DOCTYPE html>", File.ReadAllText(result.HtmlReportPath));
        Assert.Contains("\"title\": \"Openings Compliance Report\"", File.ReadAllText(result.JsonReportPath));
    }

    [Fact]
    public void Execute_opens_html_report_when_requested()
    {
        var browser = new RecordingReportBrowserLauncher();
        var service = CreateService(browser);
        var result = service.Execute(LauncherTestFixtureBuilder.CreatePassRequest(openBrowser: true));

        Assert.Equal(result.HtmlReportPath, browser.OpenedHtmlReportPath);
    }

    [Fact]
    public void Execute_pass_scenario_produces_compliant_results()
    {
        var service = CreateService(new RecordingReportBrowserLauncher());
        var result = service.Execute(LauncherTestFixtureBuilder.CreatePassRequest());

        Assert.Equal(100m, result.PipelineResult!.DoorNamingResult!.Summary!.CompliancePercentage);
        Assert.Equal(100m, result.PipelineResult.WindowNamingResult!.Summary!.CompliancePercentage);
    }

    [Fact]
    public void Execute_fail_scenario_produces_violations_and_evidence()
    {
        var service = CreateService(new RecordingReportBrowserLauncher());
        var result = service.Execute(LauncherTestFixtureBuilder.CreateFailRequest());

        Assert.Equal(LauncherRuleExecutionStatus.Completed, result.Status);
        Assert.True(result.PipelineResult!.DoorNamingResult!.Summary!.FailedChecks > 0);
        Assert.NotEmpty(result.PipelineResult.DoorNamingResult.Evidence!);
    }

    [Fact]
    public void Execute_returns_rule_load_failure_for_missing_rule_file()
    {
        var service = CreateService(new RecordingReportBrowserLauncher());
        var request = LauncherTestFixtureBuilder.CreatePassRequest() with
        {
            RuleFilePath = Path.Combine(Path.GetTempPath(), "missing-rule.bimrule")
        };

        var result = service.Execute(request);

        Assert.Equal(LauncherRuleExecutionStatus.RuleLoadFailed, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    private static LauncherRuleExecutionService CreateService(RecordingReportBrowserLauncher browser)
    {
        return new LauncherRuleExecutionService(
            new ValidationPipeline(),
            new ReportOutputWriter(),
            browser);
    }
}

public class LauncherPathResolverTests
{
    [Fact]
    public void ResolveSharedParameterFilePath_resolves_rule_relative_location()
    {
        var rulePath = LauncherTestFixtureBuilder.GetDemoRulePath();
        var loader = new BIMCapabilities.Contracts.Rules.Loading.BimRuleLoader();
        var loadResult = loader.Load(rulePath);

        var resolvedPath = LauncherPathResolver.ResolveSharedParameterFilePath(
            rulePath,
            loadResult.Rule!,
            overridePath: null);

        Assert.Equal(LauncherTestFixtureBuilder.GetDemoSharedParameterPath(), resolvedPath);
        Assert.True(File.Exists(resolvedPath));
    }

    [Fact]
    public void ResolveReportDirectory_uses_temp_bimcapabilities_folder()
    {
        var directory = LauncherPathResolver.ResolveReportDirectory("corr-demo-001");

        Assert.StartsWith(Path.Combine(Path.GetTempPath(), "BIMCapabilities"), directory);
        Assert.EndsWith("corr-demo-001", directory);
    }
}

public class ReportOutputWriterTests
{
    [Fact]
    public void Write_persists_html_and_json_files()
    {
        var writer = new ReportOutputWriter();
        var directory = Path.Combine(Path.GetTempPath(), "BIMCapabilities", Guid.NewGuid().ToString("N"));

        var paths = writer.Write(
            directory,
            "STD-ARC-OPENINGS-V01",
            new HtmlRenderResult
            {
                Html = "<!DOCTYPE html><html><body>Test</body></html>",
                FileContent = "<!DOCTYPE html><html><body>Test</body></html>",
                Title = "Test",
                ContentType = "text/html; charset=utf-8"
            },
            new JsonRenderResult
            {
                Json = "{\"title\":\"Test\"}",
                DocumentContent = "{\"title\":\"Test\"}",
                ContentType = "application/json; charset=utf-8"
            });

        Assert.True(File.Exists(paths.HtmlReportPath));
        Assert.True(File.Exists(paths.JsonReportPath));
    }
}

public class LauncherArchitectureTests
{
    [Fact]
    public void Launcher_references_composition_and_adapter_but_not_engines_directly()
    {
        var referencedAssemblies = typeof(LauncherRuleExecutionService).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("BIMCapabilities.Composition", referencedAssemblies);
        Assert.Contains("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Family", referencedAssemblies);
    }

    [Fact]
    public void Launcher_execution_service_does_not_expose_write_or_correction_methods()
    {
        var methods = typeof(LauncherRuleExecutionService)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName);

        Assert.All(methods, method =>
        {
            Assert.DoesNotContain("Write", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Correct", method.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Transaction", method.Name, StringComparison.OrdinalIgnoreCase);
        });
    }
}
