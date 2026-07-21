using BIMCapabilities.Contracts.Execution;
using BIMCapabilities.Launchers.Revit.Execution;
using BIMCapabilities.Launchers.Revit.Tests.Fixtures;

namespace BIMCapabilities.Launchers.Revit.Tests.Fixtures;

internal static class LauncherTestFixtureBuilder
{
    internal const string CorrelationId = "corr-launcher-test-001";

    internal static string GetDemoRulePath()
    {
        return Path.GetFullPath(Path.Combine(
            FindRepositoryRoot(),
            "examples",
            "demo",
            "STD-ARC-OPENINGS-V01.bimrule"));
    }

    internal static string GetDemoSharedParameterPath()
    {
        return Path.GetFullPath(Path.Combine(
            FindRepositoryRoot(),
            "examples",
            "demo",
            "CompanySharedParameters.txt"));
    }

    internal static LauncherRuleExecutionRequest CreatePassRequest(bool openBrowser = false)
    {
        return CreateRequest(new LauncherDemoFamilyProvider(passScenario: true), openBrowser);
    }

    internal static LauncherRuleExecutionRequest CreateFailRequest(bool openBrowser = false)
    {
        return CreateRequest(new LauncherDemoFamilyProvider(passScenario: false), openBrowser);
    }

    private static LauncherRuleExecutionRequest CreateRequest(
        LauncherDemoFamilyProvider provider,
        bool openBrowser)
    {
        return new LauncherRuleExecutionRequest
        {
            RuleFilePath = GetDemoRulePath(),
            FamilyProvider = provider,
            SharedParameterFilePathOverride = GetDemoSharedParameterPath(),
            Scope = new ExecutionScope
            {
                ScopeType = "EntireModel",
                TargetDescription = "Launcher test model"
            },
            Environment = new ExecutionEnvironment
            {
                Platform = "Revit",
                PlatformVersion = "2026",
                ModelName = "LauncherTest.rvt"
            },
            CorrelationId = $"corr-launcher-test-{Guid.NewGuid():N}",
            ExecutedAt = LauncherDemoFamilyProvider.FixedExecutedAt,
            OpenHtmlReportInBrowser = openBrowser
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BIMCapabilities.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
