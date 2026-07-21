using System.Reflection;
using BIMCapabilities.Composition.Fix;
using BIMCapabilities.Composition.Validation;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Launchers.Revit.Commands;
using BIMCapabilities.Launchers.Revit.Execution;
using BIMCapabilities.Launchers.Revit.Results;
using BIMCapabilities.Launchers.Revit.Tests.Fixtures;

namespace BIMCapabilities.Launchers.Revit.Tests;

/// <summary>
/// Automated API verification for BIMLinker integration boundary (Step 0).
/// Exercises public execution services without launcher UI orchestration.
/// </summary>
public class ApiVerificationTests
{
    [Fact]
    public void LauncherRuleExecutionService_executes_known_rule_without_command_orchestration()
    {
        var service = new LauncherRuleExecutionService(
            new ValidationPipeline(),
            new ReportOutputWriter(),
            new RecordingReportBrowserLauncher());

        var result = service.Execute(LauncherTestFixtureBuilder.CreatePassRequest());

        Assert.Equal(LauncherRuleExecutionStatus.Completed, result.Status);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.PipelineResult);
        Assert.True(result.PipelineResult.RuleValidationSucceeded);
    }

    [Fact]
    public void LauncherRuleExecutionResult_exposes_required_validation_outputs()
    {
        var result = ExecuteVerificationValidation();

        Assert.Equal(LauncherRuleExecutionStatus.Completed, result.Status);
        Assert.NotNull(result.PipelineResult);
        Assert.NotNull(result.HtmlReportPath);
        Assert.NotNull(result.JsonReportPath);
        Assert.NotNull(result.ReportDirectory);
        Assert.NotNull(result.CorrelationId);
        Assert.Null(result.ErrorMessage);
        Assert.True(File.Exists(result.HtmlReportPath));
        Assert.True(File.Exists(result.JsonReportPath));
        Assert.StartsWith(Path.Combine(Path.GetTempPath(), "BIMCapabilities"), result.ReportDirectory);
    }

    [Fact]
    public void LauncherRuleExecutionService_generates_reports_and_returns_paths()
    {
        var result = ExecuteVerificationValidation();

        Assert.Contains("<!DOCTYPE html>", File.ReadAllText(result.HtmlReportPath!));
        Assert.Contains("\"title\": \"Openings Compliance Report\"", File.ReadAllText(result.JsonReportPath!));
    }

    [Fact]
    public void LauncherRuleFixExecutionRequest_is_public_without_rule_fix_execution()
    {
        var requestType = typeof(LauncherRuleFixExecutionRequest);
        var fixServiceSource = File.ReadAllText(FindSourceFile("LauncherRuleFixExecutionService.cs"));
        var fixExecutionSource = File.ReadAllText(FindSourceFile("RuleFixExecution.cs"));

        Assert.True(requestType.IsPublic);
        Assert.False(typeof(RuleFixExecution).IsPublic);
        Assert.Contains("public sealed record LauncherRuleFixExecutionRequest", fixServiceSource, StringComparison.Ordinal);
        Assert.Contains("required Autodesk.Revit.DB.Document Document", fixServiceSource, StringComparison.Ordinal);
        Assert.Contains("required ValidationPipelineResult ValidationResult", fixServiceSource, StringComparison.Ordinal);
        Assert.Contains("new LauncherRuleFixExecutionRequest", fixExecutionSource, StringComparison.Ordinal);
        Assert.Contains("internal static class RuleFixExecution", fixExecutionSource, StringComparison.Ordinal);
    }

    [Fact]
    public void RuleFixExecution_is_internal_while_fix_service_is_public()
    {
        Assert.False(typeof(RuleFixExecution).IsPublic);
        Assert.True(typeof(LauncherRuleFixExecutionService).IsPublic);
    }

    [Fact]
    public void FixPipeline_builds_write_requests_without_launcher_ui()
    {
        var rulePath = GetFixDemoRulePath();
        var sharedParameterPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(rulePath)!,
            "..",
            "shared-parameters",
            "CompanySharedParameters.Revit.txt"));

        var validationResult = new ValidationPipeline().Execute(new ValidationPipelineRequest
        {
            RuleFilePath = rulePath,
            FamilyProvider = new FixVerificationFamilyProvider(),
            SharedParameterFilePathOverride = sharedParameterPath,
            Scope = new ExecutionScope { ScopeType = "EntireModel", TargetDescription = "Fix verification" },
            Environment = new ExecutionEnvironment { Platform = "Revit", PlatformVersion = "2026", ModelName = "fix-verify.rvt" }
        });

        Assert.True(validationResult.RuleValidationSucceeded);

        var fixResult = new FixPipeline().BuildWriteRequests(new FixPipelineRequest
        {
            ValidationResult = validationResult,
            RuleFilePath = rulePath,
            SharedParameterFilePathOverride = sharedParameterPath
        });

        Assert.True(fixResult.Succeeded);
        Assert.NotEmpty(fixResult.WriteRequests);
    }

    [Fact]
    public void CanApplyAutomaticCorrection_logic_is_internal_and_not_on_public_api()
    {
        var supportType = typeof(RuleDialogSupport);
        var method = supportType.GetMethod(
            "CanApplyAutomaticCorrection",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.False(method!.IsPublic);
    }

    private static LauncherRuleExecutionResult ExecuteVerificationValidation()
    {
        var service = new LauncherRuleExecutionService(
            new ValidationPipeline(),
            new ReportOutputWriter(),
            new RecordingReportBrowserLauncher());

        return service.Execute(LauncherTestFixtureBuilder.CreatePassRequest());
    }

    private static string GetFixDemoRulePath()
    {
        var openingsRulePath = LauncherTestFixtureBuilder.GetDemoRulePath();
        var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(openingsRulePath)!, "..", ".."));
        return Path.Combine(repositoryRoot, "samples", "rules", "DEMO-RULE-FIX-DOORS.bimrule");
    }

    private static string FindSourceFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var match = Directory.GetFiles(directory.FullName, fileName, SearchOption.AllDirectories).FirstOrDefault();
            if (match is not null)
            {
                return match;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate source file {fileName}.");
    }

    private sealed class FixVerificationFamilyProvider : IFamilyProvider
    {
        public FamilyQueryResult Retrieve(FamilyQuery query)
        {
            var families = new[]
            {
                new NormalizedFamily
                {
                    Identity = new NormalizedIdentifier { Id = "family-door-fix-001", Kind = "family" },
                    Name = "DR_SingleDoor",
                    Category = new NormalizedCategory
                    {
                        Identifier = new NormalizedIdentifier { Id = "category-doors", Kind = "category" },
                        Name = "Doors"
                    },
                    FamilyTypes =
                    [
                        new NormalizedFamilyType
                        {
                            Identity = new NormalizedIdentifier { Id = "family-type-door-fix-001", Kind = "familyType" },
                            Name = "DR_SingleDoor900x2100",
                            Parameters = []
                        }
                    ]
                }
            };

            return new FamilyQueryResult
            {
                Families = families,
                QueryMetadata = new FamilyQueryMetadata
                {
                    ExecutedAt = DateTimeOffset.UtcNow,
                    ProviderId = "fix-verification-provider"
                }
            };
        }
    }
}
